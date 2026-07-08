shader_type canvas_item;
render_mode unshaded, blend_premul_alpha;

// ───────── 可调参数 ─────────
uniform float refractive_index : hint_range(1.0, 3.0) = 1.8;
uniform float dispersion       : hint_range(0.0, 1.0) = 0.35;
uniform float sparkle_power    : hint_range(0.0, 3.0) = 1.2;
uniform vec3  crystal_tint     : source_color = vec3(0.85, 0.92, 1.0); // 默认水晶色（当节点颜色为白色时使用）
uniform float fresnel_power    : hint_range(1.0, 5.0) = 2.5;
uniform float time_speed       : hint_range(0.0, 3.0) = 1.0;
uniform float highlight_size   : hint_range(8.0, 64.0) = 32.0;
uniform float black_threshold  : hint_range(0.0, 0.5) = 0.08;

// ───────── 辅助函数 ────────
vec3 rgb2hsl(vec3 rgb) {
    float maxVal = max(max(rgb.r, rgb.g), rgb.b);
    float minVal = min(min(rgb.r, rgb.g), rgb.b);
    float delta = maxVal - minVal;
    float h = 0.0, s = 0.0, l = (maxVal + minVal) / 2.0;
    if (delta > 0.0) {
        s = l < 0.5 ? delta / (maxVal + minVal) : delta / (2.0 - maxVal - minVal);
        if (maxVal == rgb.r) h = mod((rgb.g - rgb.b) / delta, 6.0);
        else if (maxVal == rgb.g) h = (rgb.b - rgb.r) / delta + 2.0;
        else h = (rgb.r - rgb.g) / delta + 4.0;
        h /= 6.0;
    }
    return vec3(h, s, l);
}

vec3 hsl2rgb(vec3 hsl) {
    float H = hsl.x * 360.0, S = hsl.y, L = hsl.z;
    float C = (1.0 - abs(2.0 * L - 1.0)) * S;
    float X = C * (1.0 - abs(mod(H / 60.0, 2.0) - 1.0));
    float m = L - C / 2.0;
    vec3 rgb;
    if (H < 60.0) rgb = vec3(C, X, 0.0);
    else if (H < 120.0) rgb = vec3(X, C, 0.0);
    else if (H < 180.0) rgb = vec3(0.0, C, X);
    else if (H < 240.0) rgb = vec3(0.0, X, C);
    else if (H < 300.0) rgb = vec3(X, 0.0, C);
    else rgb = vec3(C, 0.0, X);
    return rgb + m;
}

void fragment() {
    vec2 uv = UV;
    vec2 center = uv - 0.5;
    vec4 tex = texture(TEXTURE, uv);
    
    // 1. 去除黑底
    float brightness = (tex.r + tex.g + tex.b) / 3.0;
    if (brightness < black_threshold || tex.a < 0.01) {
        discard;
    }

    float aspect = TEXTURE_PIXEL_SIZE.y / TEXTURE_PIXEL_SIZE.x;
    vec2 p = center * vec2(1.0, aspect);

    // 2. 读取节点颜色 (Modulate) 作为随机种子
    // 如果用户在编辑器里把颜色改成了随机色，这里就会读到那个颜色
    vec3 instance_color = COLOR.rgb;
    
    // 如果颜色是纯白 (1,1,1)，说明用户没改过，使用默认水晶色
    if (instance_color == vec3(1.0)) {
        instance_color = crystal_tint;
    }

    // 3. 基础颜色合成
    vec3 base_color = tex.rgb * instance_color;

    // 4. 法线与菲涅尔
    vec2 t = TEXTURE_PIXEL_SIZE;
    vec3 N = normalize(vec3(
        texture(TEXTURE, uv - vec2(t.x, 0.0)).a - texture(TEXTURE, uv + vec2(t.x, 0.0)).a,
        texture(TEXTURE, uv - vec2(0.0, t.y)).a - texture(TEXTURE, uv + vec2(0.0, t.y)).a,
        0.15
    ));
    vec3 V = normalize(vec3(center * 4.0, 1.0));
    float fresnel = pow(1.0 - abs(dot(V, N)), fresnel_power);

    // 5. 彩虹色散 (基于实例颜色进行微调，让不同颜色的水晶色散也不同)
    float t_time = TIME * time_speed;
    float a = atan(p.y, p.x);
    vec3 rainbow_col = vec3(
        sin(a * 3.0 + t_time * 0.7) * 0.5 + 0.5,
        sin(a * 3.0 + t_time * 0.7 + 2.094) * 0.5 + 0.5,
        sin(a * 3.0 + t_time * 0.7 + 4.189) * 0.5 + 0.5
    );
    // 将彩虹色与实例颜色混合，产生独特的色散效果
    vec3 dispersion_col = mix(rainbow_col, instance_color, 0.5) * dispersion * fresnel;

    // 6. 动态闪烁
    float sparkle = 0.0;
    for (int i = 0; i < 4; i++) {
        float angle = float(i) * 1.5708 + t_time * 0.5;
        vec2 spark_dir = vec2(cos(angle), sin(angle));
        float d = dot(normalize(p + vec2(0.0001)), spark_dir);
        sparkle += pow(max(0.0, d), 16.0) * 0.25;
    }
    sparkle *= sparkle_power;

    // 7. 内部折射
    vec2 refract_uv = 0.5 + center / refractive_index * vec2(1.0, aspect);
    vec3 refract_color = texture(TEXTURE, clamp(refract_uv, 0.0, 1.0)).rgb * instance_color;

    // 8. 镜面高光
    vec3 L = normalize(vec3(0.4, 0.6, 1.0));
    vec3 H = normalize(V + L);
    float spec = pow(max(0.0, dot(N, H)), highlight_size);

    // 9. 最终合成
    vec3 col = base_color;
    col += dispersion_col * 1.5;
    col += sparkle * vec3(1.0, 0.97, 0.92);
    col += spec * vec3(1.0);
    col += refract_color * 0.25;
    col += fresnel * instance_color * 0.6; // 边缘光也跟随实例颜色

    COLOR = vec4(col, tex.a);
}
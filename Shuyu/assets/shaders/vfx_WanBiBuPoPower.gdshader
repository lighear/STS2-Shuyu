shader_type canvas_item;

// ========== 基础参数 ==========
uniform float time_speed : hint_range(0.1, 5.0) = 1.0;        // 整体动画速度
uniform float ice_tint : hint_range(0.0, 1.0) = 0.35;         // 冰蓝染色强度
uniform vec3 ice_color : source_color = vec3(0.55, 0.85, 1.0);// 冰色主色

// ========== 边缘辉光（坚固感） ==========
uniform float rim_power : hint_range(0.5, 5.0) = 2.2;
uniform float rim_strength : hint_range(0.0, 3.0) = 1.6;
uniform vec3 rim_color : source_color = vec3(0.8, 0.95, 1.0);

// ========== 扫光高光（镜面反射） ==========
uniform float sweep_interval : hint_range(0.5, 6.0) = 5.0;    // 两次扫光间隔（秒）
uniform float sweep_width : hint_range(0.02, 0.4) = 0.12;     // 高光带宽度
uniform float sweep_sharpness : hint_range(1.0, 16.0) = 6.0;  // 高光锐度
uniform float sweep_strength : hint_range(0.0, 4.0) = 0.5;    // 高光强度

// ========== 冰晶微光（晶莹感） ==========
uniform float sparkle_strength : hint_range(0.0, 1.5) = 0.6;  // 光斑强度
uniform float sparkle_size : hint_range(0.001, 0.05) = 0.03;  // 光斑大小
uniform int sparkle_count : hint_range(1, 20) = 6;            // 光斑数量

// ---------- 基础哈希函数 ----------
vec2 hash(vec2 p) {
    float h = dot(p, vec2(127.1, 311.7));
    return fract(sin(vec2(h, h + 1.0)) * 43758.5453);
}

void fragment() {
    float t = TIME * time_speed;
    vec2 uv = UV;
    vec2 sample_uv = clamp(uv, vec2(0.0), vec2(1.0));
    vec4 tex = texture(TEXTURE, sample_uv);

    if (tex.a < 0.01) {
        discard;
    }

    // ---------- 1. 冰蓝染色 ----------
    vec3 col = mix(tex.rgb, tex.rgb * ice_color * 1.4, ice_tint);
    col *= 1.08;

    // ---------- 2. 边缘辉光（护盾坚固感） ----------
    vec2 texel = TEXTURE_PIXEL_SIZE;
    float aL = texture(TEXTURE, sample_uv - vec2(texel.x, 0.0)).a;
    float aR = texture(TEXTURE, sample_uv + vec2(texel.x, 0.0)).a;
    float aU = texture(TEXTURE, sample_uv + vec2(0.0, texel.y)).a;
    float aD = texture(TEXTURE, sample_uv - vec2(0.0, texel.y)).a;
    float edge = clamp(abs(aR - aL) + abs(aU - aD), 0.0, 1.0);
    float rim = pow(edge, 1.0 / rim_power) * rim_strength;
    col += rim_color * rim;

    // ---------- 3. 右上→左下 周期性扫光 ----------
    vec2 sweep_dir = normalize(vec2(-1.0, 1.0));
    float proj = dot(uv - vec2(0.5), sweep_dir);
    float phase = mod(t, sweep_interval) / sweep_interval;
    float sweep_center = mix(-1.2, 1.2, phase);
    float band = exp(-pow((proj - sweep_center) / sweep_width, 2.0) * sweep_sharpness);
    float core = exp(-pow((proj - sweep_center) / (sweep_width * 0.25), 2.0) * sweep_sharpness * 2.0);
    vec3 sweep_col = vec3(0.85, 0.95, 1.0) * band * sweep_strength
                   + vec3(1.0, 1.0, 1.0) * core * sweep_strength * 0.8;
    col += sweep_col * tex.a;

    // ---------- 4. 冰晶微光（柔和浮动优化） ----------
    float sparkle_sum = 0.0;
    for (int i = 0; i < sparkle_count; i++) {
        float seed = float(i) * 0.6723 + 0.11;

        // 固定基础位置
        vec2 base = hash(vec2(seed, 0.0));

        // 连续缓慢的浮动偏移，避免跳变
        vec2 offset;
        offset.x = sin(t * 0.7 + seed * 7.0) * 0.12 + cos(t * 0.5 + seed * 9.0) * 0.08;
        offset.y = cos(t * 0.8 + seed * 8.0) * 0.12 + sin(t * 0.6 + seed * 10.0) * 0.08;

        vec2 center = base + offset;
        // 如果浮出边界则循环回另一侧，保持光斑始终可见
        center = fract(center);

        float dist = length(uv - center);
        float glow = exp(-dist * dist / (sparkle_size * sparkle_size));

        // 柔和呼吸式亮度变化，代替原来高频闪烁
        float brightness = 0.7 + 0.3 * sin(t * 1.3 + seed * 5.0 + float(i) * 0.2);
        glow *= brightness;

        sparkle_sum += glow;
    }
    float sparkle = sparkle_sum * sparkle_strength * 0.3;
    col += vec3(0.9, 1.0, 1.0) * sparkle * tex.a;

    // ---------- 输出 ----------
    COLOR = vec4(col, tex.a);
}
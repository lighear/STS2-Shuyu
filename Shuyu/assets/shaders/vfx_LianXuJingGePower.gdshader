shader_type canvas_item;

// ============ 颜色控制 ============
uniform vec4 color_normal : source_color = vec4(0.3, 0.75, 1.0, 1.0);
uniform vec4 color_active : source_color = vec4(1.0, 0.2, 0.25, 1.0);
uniform float active : hint_range(0.0, 1.0) = 0.0;

// ============ 六边形网格 ============
uniform float hex_count : hint_range(2.0, 30.0) = 10.0;
uniform float edge_width : hint_range(0.01, 0.25) = 0.06; // 稍微收窄，线条更锐利

// ============ 动画参数 ============
uniform float pulse_speed : hint_range(0.0, 10.0) = 2.5;
uniform float pulse_frequency : hint_range(0.1, 20.0) = 6.0;
uniform float breath_speed : hint_range(0.0, 5.0) = 1.5;

// ============ 视觉参数 ============
uniform float curvature : hint_range(0.0, 1.0) = 0.25;
uniform float glow_intensity : hint_range(0.0, 3.0) = 2.0;
uniform float base_alpha : hint_range(0.0, 1.0) = 0.06;   // 内部填充更淡，突出边线
uniform float falloff_power : hint_range(0.5, 4.0) = 2.0;

// ---------- 工具函数 ----------

// 六边形网格：返回当前像素到最近六边形中心的向量（UV 空间已缩放）
vec2 hexGrid(vec2 uv) {
    vec2 r = vec2(1.0, 1.7320508);  // (1, sqrt(3))
    vec2 h = r * 0.5;
    vec2 a = mod(uv, r) - h;
    vec2 b = mod(uv - h, r) - h;
    return dot(a, a) < dot(b, b) ? a : b;
}

// 修正后：尖顶六边形边界距离（内部为正，外部为负）
float hexEdgeDist(vec2 p) {
    vec2 ap = abs(p);
    // 尖顶六边形：max(|x|, 0.5*|x| + 0.866*|y|) <= 0.5
    return 0.5 - max(ap.x, ap.x * 0.5 + ap.y * 0.8660254);
}

// ---------- 主函数 ----------

void fragment() {
    // 1. UV 居中到 [-1, 1]
    vec2 uv = (UV - 0.5) * 2.0;
    float dist_center = length(uv);

    // 2. 弧度变形
    vec2 curved_uv = uv * (1.0 + curvature * dist_center * dist_center);

    // 3. 六边形网格采样
    vec2 pixel_uv = curved_uv * hex_count;
    vec2 gv = hexGrid(pixel_uv);
    float edge_dist = hexEdgeDist(gv);
    float center_dist = length(gv);

    // 4. 六边形边线（锐利发光线条）
    float edge_line = 1.0 - smoothstep(0.0, edge_width, edge_dist);

    // 5. 从圆心向外扩散的能量波动
    float wave = sin(dist_center * pulse_frequency - TIME * pulse_speed);
    wave = wave * 0.5 + 0.5;
    wave = pow(wave, 2.0);

    // 6. 整体呼吸效果
    float breath = sin(TIME * breath_speed) * 0.3 + 0.7;

    // 7. 六边形内部微弱填充（模拟能量单元）
    float cell_fill = exp(-center_dist * 4.0) * 0.12;

    // 8. 中心高光（暗示能量源）
    float center_glow = exp(-dist_center * 2.5) * 0.25 * breath;

    // 9. 径向衰减（塑造圆形轮廓）
    float falloff = 1.0 - pow(clamp(dist_center, 0.0, 1.0), falloff_power);
    falloff = smoothstep(0.0, 1.0, falloff);

    // 10. 合成
    float edge_glow = edge_line * wave * glow_intensity * breath;
    float alpha = (edge_glow + cell_fill * breath + center_glow) * falloff;

    // 11. 颜色混合
    vec3 base_color = mix(color_normal.rgb, color_active.rgb, active);
    vec3 final_rgb = base_color * (edge_glow * 1.2 + cell_fill * 0.6 + center_glow);

    COLOR = vec4(final_rgb, alpha);
}
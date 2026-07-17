shader_type canvas_item;

// ============ 颜色控制 ============
uniform vec4 color_normal : source_color = vec4(0.3, 0.75, 1.0, 1.0);
uniform vec4 color_active : source_color = vec4(1.0, 0.2, 0.25, 1.0);
uniform float active : hint_range(0.0, 1.0) = 0.0;

// ============ 六边形网格 ============
uniform float hex_count : hint_range(2.0, 30.0) = 10.0;
uniform float edge_width : hint_range(0.01, 0.25) = 0.06;

// ============ 动画参数 ============
uniform float pulse_speed : hint_range(0.0, 10.0) = 2.5;
uniform float pulse_frequency : hint_range(0.1, 20.0) = 6.0;
uniform float breath_speed : hint_range(0.0, 5.0) = 1.5;
uniform float flicker_speed : hint_range(0.1, 10.0) = 2.0;  // 激活时的闪烁速度

// ============ 视觉参数 ============
uniform float curvature : hint_range(0.0, 1.0) = 0.25;
uniform float glow_intensity : hint_range(0.0, 3.0) = 2.0;
uniform float base_alpha : hint_range(0.0, 1.0) = 0.06;     // 六边形内部填充强度（极淡）
uniform float opacity : hint_range(0.0, 1.0) = 0.4;         // 全局不透明度（降低遮挡）
uniform float falloff_power : hint_range(0.5, 4.0) = 2.0;

// ---------- 工具函数 ----------

vec2 hexGrid(vec2 uv) {
    vec2 r = vec2(1.0, 1.7320508); // (1, sqrt(3))
    vec2 h = r * 0.5;
    vec2 a = mod(uv, r) - h;
    vec2 b = mod(uv - h, r) - h;
    return dot(a, a) < dot(b, b) ? a : b;
}

float hexEdgeDist(vec2 p) {
    vec2 ap = abs(p);
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

    // 4. 六边形边线
    float edge_line = 1.0 - smoothstep(0.0, edge_width, edge_dist);

    // 5. 动态效果：active=0 时是简单的全局闪烁，active=1 时是原来的空间波纹
    float wave_raw = sin(dist_center * pulse_frequency - TIME * pulse_speed);
    wave_raw = wave_raw * 0.5 + 0.5;
    float wave_pow = pow(wave_raw, 2.0);

    float flicker = abs(sin(TIME * flicker_speed)) - 0.5;   // 简单闪烁
    float final_wave = mix(flicker, wave_pow, active);       // 根据激活程度混合

    // 6. 整体呼吸效果
    float breath = sin(TIME * breath_speed) * 0.3 + 0.7;

    // 7. 六边形内部微弱填充（现在使用 base_alpha 控制强度）
    float cell_fill = exp(-center_dist * 4.0) * base_alpha;

    // 8. 中心高光
    float center_glow = exp(-dist_center * 2.5) * 0.25 * breath;

    // 9. 径向衰减
    float falloff = 1.0 - pow(clamp(dist_center, 0.0, 1.0), falloff_power);
    falloff = smoothstep(0.0, 1.0, falloff);

    // 10. 合成
    float edge_glow = edge_line * final_wave * glow_intensity * breath;
    float alpha = (edge_glow + cell_fill * breath + center_glow) * falloff;

    // 11. 颜色混合
    vec3 base_color = mix(color_normal.rgb, color_active.rgb, active);
    vec3 final_rgb = base_color * (edge_glow * 1.2 + cell_fill * 0.6 + center_glow);

    // 12. 全局不透明度控制，减少对背景立绘的遮挡
    COLOR = vec4(final_rgb, alpha * opacity);
}
shader_type canvas_item;

// --- 纹理与流动参数 ---
// 采样噪声纹理，开启线性过滤和重复平铺
uniform sampler2D noise_texture : source_color, filter_linear_mipmap, repeat_enable;
uniform vec2 scroll_speed = vec2(0.05, 0.02); // 流动方向和速度 (X为水平, Y为垂直)
uniform float noise_scale = 2.0;              // 噪声缩放，值越小雾气斑块越大

// --- 雾气外观参数 ---
uniform vec4 fog_color : source_color = vec4(0.85, 0.9, 0.95, 1.0); // 雾气颜色（默认偏冷白）
uniform float fog_density = 0.8;            // 整体透明度/浓度 (0.0 ~ 1.0)
uniform float noise_threshold = 0.35;       // 噪声阈值，控制雾气开始显现的浓度
uniform float noise_softness = 0.25;        // 噪声边缘柔和度，值越大雾气边缘越模糊

// --- 边缘融合参数 ---
uniform float edge_fade_width = 0.25;       // 边缘淡出宽度 (0.0 ~ 0.5)，值越大边缘过渡区域越宽

// --- 扰动参数（新增）---
uniform float disturbance_strength = 0.03; // 扰动强度，控制UV偏移幅度
uniform float disturbance_frequency = 0.8; // 扰动频率，值越高扭曲越细碎

void fragment() {
    // 1. 计算边缘淡出因子
    float fade_left   = smoothstep(0.0, edge_fade_width, UV.x);
    float fade_right  = smoothstep(1.0, 1.0 - edge_fade_width, UV.x);
    float fade_top    = smoothstep(0.0, edge_fade_width, UV.y);
    float fade_bottom = smoothstep(1.0, 1.0 - edge_fade_width, UV.y);
    float edge_fade = fade_left * fade_right * fade_top * fade_bottom;

    // 2. 生成扰动偏移量（基于正弦噪声，具有随时间流动的特性）
    //    使用不同频率和相位的正弦组合，产生类似湍流的扭曲效果
    float dist_phase = UV.x * 3.5 + UV.y * 2.7 + TIME * 0.2;
    vec2 disturbance = vec2(
        sin(dist_phase * disturbance_frequency) * cos(dist_phase * 1.3 + 1.2) * disturbance_strength,
        cos(dist_phase * disturbance_frequency * 0.9 + 2.4) * sin(dist_phase * 1.7) * disturbance_strength
    );

    // 3. 采样噪声并加入扰动
    // 基础层噪声：大块雾气流动，UV加上扰动偏移
    vec2 scrolled_uv1 = UV * noise_scale + TIME * scroll_speed * 0.1 + disturbance;
    float noise1 = texture(noise_texture, scrolled_uv1).r;

    // 细节层噪声：同样加入扰动（可以略作缩放差异）
    vec2 scrolled_uv2 = UV * (noise_scale * 2.5) + TIME * scroll_speed * 0.15 + vec2(0.5) + disturbance * 1.2;
    float noise2 = texture(noise_texture, scrolled_uv2).r;

    // 混合两层噪声
    float final_noise = mix(noise1, noise2, 0.4);

    // 4. 调整噪声对比度
    final_noise = smoothstep(noise_threshold, noise_threshold + noise_softness, final_noise);

    // 5. 计算最终透明度并输出颜色
    float alpha = final_noise * edge_fade * fog_density;
    COLOR = vec4(fog_color.rgb, alpha);
}
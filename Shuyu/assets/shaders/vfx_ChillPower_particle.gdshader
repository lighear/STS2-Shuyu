shader_type canvas_item;

// === 基础 ===
// 整体透明度
uniform float opacity : hint_range(0.0, 1.0) = 1.0;

// === 模糊 (Blur) ===
// 启用模糊
uniform bool enable_blur = false;
// 模糊半径
uniform float blur_radius : hint_range(0.0, 20.0) = 2.0;
// 高斯模糊方向 (0=双向, 1=水平, 2=垂直)
uniform int blur_direction : hint_range(0, 2) = 0;
// 运动模糊角度
uniform float motion_blur_angle : hint_range(0.0, 360.0) = 0.0;
// 运动模糊距离
uniform float motion_blur_distance : hint_range(0.0, 20.0) = 0.0;

// =============================================
// 模糊算法函数
// =============================================

// 高质量高斯模糊 (支持单向或双向)
vec4 gaussian_blur(sampler2D tex, vec2 uv, vec2 pixel_size, float radius, int direction) {
    if (radius <= 0.0) return texture(tex, uv);

    vec4 color = vec4(0.0);
    float total_weight = 0.0;

    int samples = int(min(radius * 2.0 + 1.0, 32.0));

    for (int i = -samples; i <= samples; i++) {
        float fi = float(i);
        float weight = exp(-(fi * fi) / (2.0 * radius * radius + 0.001));

        vec2 offset = vec2(0.0);
        if (direction == 0 || direction == 1) offset.x = fi * pixel_size.x;
        if (direction == 0 || direction == 2) offset.y = fi * pixel_size.y;

        color += texture(tex, uv + offset) * weight;
        total_weight += weight;
    }

    return color / total_weight;
}

// 双向高斯模糊 (分两步模拟：先水平后垂直取平均)
vec4 gaussian_blur_2d(sampler2D tex, vec2 uv, vec2 pixel_size, float radius) {
    if (radius <= 0.0) return texture(tex, uv);

    vec4 horizontal = vec4(0.0);
    float total_weight = 0.0;
    int samples = int(min(radius * 2.0 + 1.0, 32.0));

    for (int i = -samples; i <= samples; i++) {
        float fi = float(i);
        float weight = exp(-(fi * fi) / (2.0 * radius * radius + 0.001));
        horizontal += texture(tex, uv + vec2(fi * pixel_size.x, 0.0)) * weight;
        total_weight += weight;
    }
    horizontal /= total_weight;

    vec4 vertical = vec4(0.0);
    total_weight = 0.0;
    for (int i = -samples; i <= samples; i++) {
        float fi = float(i);
        float weight = exp(-(fi * fi) / (2.0 * radius * radius + 0.001));
        vertical += texture(tex, uv + vec2(0.0, fi * pixel_size.y)) * weight;
        total_weight += weight;
    }
    vertical /= total_weight;

    return (horizontal + vertical) * 0.5;
}

// 运动模糊
vec4 motion_blur(sampler2D tex, vec2 uv, vec2 pixel_size, float distance, float angle_deg) {
    if (distance <= 0.0) return texture(tex, uv);

    float angle = radians(angle_deg);
    vec2 dir = vec2(cos(angle), sin(angle)) * pixel_size * distance;

    vec4 color = vec4(0.0);
    int samples = 16;
    for (int i = 0; i < samples; i++) {
        float t = float(i) / float(samples - 1);
        color += texture(tex, uv + dir * (t - 0.5));
    }
    return color / float(samples);
}

// =============================================
// Main
// =============================================

void fragment() {
    vec2 pixel_size = TEXTURE_PIXEL_SIZE;
    vec2 uv = UV;

    // 获取粒子系统赋予的原始颜色（顶点颜色）
    // 在 fragment 函数开始时，COLOR 变量保存了粒子系统计算出的颜色（包含 color_ramp 等）
    vec4 particle_color = COLOR;

    // 1. 模糊采样
    vec4 blur = vec4(0.0);
    if (enable_blur) {
        if (motion_blur_distance > 0.0) {
            blur = motion_blur(TEXTURE, uv, pixel_size, motion_blur_distance, motion_blur_angle);
        } else if (blur_direction == 0) {
            blur = gaussian_blur_2d(TEXTURE, uv, pixel_size, blur_radius);
        } else {
            blur = gaussian_blur(TEXTURE, uv, pixel_size, blur_radius, blur_direction);
        }
        // 将模糊结果染上粒子的颜色
        blur *= particle_color;
    }

    // 2. 合成基础颜色
    vec4 base_color = texture(TEXTURE, uv) * particle_color;

    // 如果启用模糊，混合模糊效果
    if (enable_blur) {
        // 提示：1.0 表示完全使用模糊结果；
        // 如果你想保留一点原图的锐利边缘，可以将其改为 0.8 或 0.9
        base_color = mix(base_color, blur, 1.0);
    }

    // 3. 应用整体透明度
    base_color.a *= opacity;

    // 最终输出
    COLOR = base_color;
}
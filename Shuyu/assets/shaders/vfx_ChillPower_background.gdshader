shader_type canvas_item;

render_mode blend_mix;  // 与背景混合

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

// --- 引入噪声贴图 ---
// 必须加上 repeat_enable，这样 UV 在流动超出 0~1 范围时才会无缝循环，而不是拉伸或截断
uniform sampler2D noise_texture : source_color, repeat_enable; 

// --- 基础参数 ---
uniform float MistIntensity : hint_range(0.0, 1.0) = 0.6;      // 雾气基础强度
uniform vec4 MistColor : source_color = vec4(0.7, 0.8, 1.0, 1.0); // 冰雾颜色（淡蓝白）

// --- 贴图流动与形态控制 ---
uniform float NoiseScale1 : hint_range(0.1, 10.0) = 2.0;       // 第一层雾气缩放 (越大越细碎)
uniform vec2 FlowSpeed1 = vec2(0.05, 0.02);                    // 第一层流动速度 (X, Y)
uniform float NoiseScale2 : hint_range(0.1, 10.0) = 3.5;       // 第二层雾气缩放
uniform vec2 FlowSpeed2 = vec2(-0.03, 0.04);                   // 第二层流动速度 (建议方向相反)
uniform float MixRatio : hint_range(0.0, 1.0) = 0.5;           // 两层雾气的混合比例
uniform float NoiseContrast : hint_range(0.1, 3.0) = 1.5;      // 雾气团块对比度 (让雾气更有形状，而不是灰蒙蒙一片)

// --- 呼吸首末端控制 ---
uniform float BreathMin : hint_range(0.0, 1.0) = 0.2;          // 呼吸最弱时的强度下限
uniform float BreathMax : hint_range(0.0, 1.0) = 1.0;          // 呼吸最强时的强度上限
uniform float BreathSpeed : hint_range(0.1, 2.0) = 0.5;        // 呼吸速度

// --- 边缘融合控制 ---
uniform float EdgeFade : hint_range(0.0, 0.5) = 0.15;          // 边缘羽化过渡范围

void fragment() {
    // 1. 采样屏幕原始画面
    vec4 screen_color = texture(SCREEN_TEXTURE, SCREEN_UV);

    // 2. 基于贴图的双层流动
    // 计算随时间移动的 UV 坐标
    vec2 uv1 = UV * NoiseScale1 + TIME * FlowSpeed1;
    vec2 uv2 = UV * NoiseScale2 + TIME * FlowSpeed2;

    // 采样贴图 (假设是灰度图，取 R 通道)
    float mist1 = texture(noise_texture, uv1).r;
    float mist2 = texture(noise_texture, uv2).r;

    // 混合两层贴图
    float mist_pattern = mix(mist1, mist2, MixRatio);
    
    // 增加对比度，让雾气呈现出明显的“团块”和“空洞”感
    mist_pattern = smoothstep(0.5 - (0.5 / NoiseContrast), 0.5 + (0.5 / NoiseContrast), mist_pattern);

    // 3. 呼吸效果 (限制在 BreathMin 和 BreathMax 之间)
    float breath_raw = 0.5 + 0.5 * sin(TIME * BreathSpeed * 2.0); 
    float dynamic_intensity = MistIntensity * mix(BreathMin, BreathMax, breath_raw);

    // 4. 边缘羽化遮罩 (消除矩形突兀感)
    float edge_x = smoothstep(0.0, EdgeFade, UV.x) * smoothstep(1.0, 1.0 - EdgeFade, UV.x);
    float edge_y = smoothstep(0.0, EdgeFade, UV.y) * smoothstep(1.0, 1.0 - EdgeFade, UV.y);
    float edge_mask = edge_x * edge_y; 

    // 5. 屏幕发光增强 (亮部额外发光)
    float screen_luminance = dot(screen_color.rgb, vec3(0.299, 0.587, 0.114));
    float glow_boost = smoothstep(0.5, 0.9, screen_luminance) * 0.3; 

    // 6. 计算最终雾的透明度，并乘以边缘遮罩
    float mist_alpha = (mist_pattern * dynamic_intensity + glow_boost) * edge_mask;
    mist_alpha = clamp(mist_alpha, 0.0, 1.0);

    // 7. 屏幕模糊处理 (模拟雪天朦胧感)
    vec2 texel_size = 1.0 / vec2(textureSize(SCREEN_TEXTURE, 0));
    vec4 blur_color = vec4(0.0);
    blur_color += texture(SCREEN_TEXTURE, SCREEN_UV + vec2(-1.0, -1.0) * texel_size * 2.0);
    blur_color += texture(SCREEN_TEXTURE, SCREEN_UV + vec2( 1.0, -1.0) * texel_size * 2.0);
    blur_color += texture(SCREEN_TEXTURE, SCREEN_UV + vec2(-1.0,  1.0) * texel_size * 2.0);
    blur_color += texture(SCREEN_TEXTURE, SCREEN_UV + vec2( 1.0,  1.0) * texel_size * 2.0);
    blur_color += screen_color;
    blur_color /= 5.0;

    // 模糊因子也受边缘遮罩控制，确保边缘不模糊
    float blur_factor = dynamic_intensity * 0.5 * edge_mask;  
    vec3 final_screen = mix(screen_color.rgb, blur_color.rgb, blur_factor);

    // 8. 叠加冰雾颜色
    vec3 mist_rgb = MistColor.rgb * mist_alpha;
    vec3 result = final_screen + mist_rgb;
    
    // 防止叠加后过曝
    result = clamp(result, 0.0, 1.0); 

    COLOR = vec4(result, mist_alpha);
}
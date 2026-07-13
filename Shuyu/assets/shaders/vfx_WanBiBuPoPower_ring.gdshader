shader_type canvas_item;

// --- 基础参数 ---
uniform float radius = 0.9;       // 圆环半径
uniform float thickness = 0.01;   // 圆环粗细
uniform vec4 shield_color : source_color = vec4(1.0, 1.0, 1.0, 1.0); // 颜色

// --- 动态效果参数 ---
uniform float breathe_speed = 1.0;  // 呼吸速度
uniform float breathe_amount = 0.01; // 呼吸幅度（半径变化量）
uniform float flow_speed = 4.0;     // 能量流动速度
uniform float flow_blobs = 3.0;     // 圆环上的光斑数量（整数效果最好，如3或4）

void fragment() {
    // 1. 基础坐标计算
    vec2 uv = UV - vec2(0.5);
    float dist = length(uv) * 2.0; 
    
    // 2. 【呼吸效果】：半径随时间正弦波动
    float dynamic_radius = radius + sin(TIME * breathe_speed) * breathe_amount;
    
    float ring_dist = abs(dist - dynamic_radius);
    
    // 3. 基础圆环形状 (作为遮罩)
    float ring_shape = smoothstep(thickness, 0.0, ring_dist);
    
    // 4. 【能量流动效果】
    // 计算当前像素相对于中心的角度 (-PI 到 PI)
    float angle = atan(uv.y, uv.x);
    // 利用 sin 函数和 TIME 制造沿圆环移动的波纹
    // flow_blobs 决定了有几个光斑，TIME * flow_speed 决定了旋转速度
    float flow = sin(angle * flow_blobs - TIME * flow_speed) * 0.5 + 0.5;
    // 使用 pow 让光斑变得更锐利、更集中
    flow = pow(flow, 6.0); 
    
    // 5. 外层光晕 (Bloom 发光源)
    float glow = 0.05 / (ring_dist + 0.01); 
    glow = clamp(glow, 0.0, 5.0); 
    
    // 将流动效果限制在圆环上，去掉连接中心的射线
    float flow_mask = smoothstep(thickness * 3.0, 0.0, ring_dist);
	float flow_on_ring = flow * flow_mask;
    
    // 6. 组合最终颜色
    // 将基础圆环、光晕和流动光斑叠加
    // 注意：把原来的 flow 换成了 flow_on_ring
    vec3 final_rgb = shield_color.rgb * (ring_shape + glow * 0.5 + flow_on_ring * 1.5);
    
    // 计算透明度 (同样使用 flow_on_ring)
    float final_alpha = ring_shape + glow * 0.3 + flow_on_ring * 0.5;
    
    COLOR = vec4(final_rgb, final_alpha);
}
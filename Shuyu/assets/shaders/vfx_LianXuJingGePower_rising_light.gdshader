shader_type canvas_item;
render_mode blend_add, unshaded;

uniform vec4 light_color : source_color = vec4(0.28, 0.72, 1.0, 1.0);
uniform float active : hint_range(0.0, 1.0) = 0.0;
uniform float growth : hint_range(0.0, 1.0) = 0.0;
uniform float opacity : hint_range(0.0, 1.0) = 0.48;
uniform float breath_speed : hint_range(0.0, 4.0) = 1.15;
uniform float ray_count : hint_range(16.0, 80.0) = 56.0;
uniform float ellipse_width_ratio : hint_range(0.1, 1.0) = 0.94;
uniform float ground_curve_ratio : hint_range(0.0, 1.0) = 0.115;
uniform float max_length_multiplier : hint_range(1.0, 4.0) = 3.0;
uniform float max_brightness_multiplier : hint_range(1.0, 3.0) = 1.3;

void fragment() {
    float fine_streaks = sin(UV.x * ray_count * 6.2831853);
    float crossed_streaks = sin(UV.x * ray_count * 3.07 + 1.4);
    float streak_light = 0.82 + fine_streaks * 0.10 + crossed_streaks * 0.08;

    // Several continuous waves vary the ray tips without breaking the dense
    // light curtain into visibly separate bars. The two high-frequency waves
    // travel at different speeds, so every local group keeps changing length.
    float length_time = TIME * 0.78;
    float dynamic_length = sin(UV.x * ray_count * 1.17 + length_time);
    dynamic_length += sin(UV.x * ray_count * 2.63 - length_time * 1.41) * 0.5;

    float soft_height = 0.76;
    soft_height += sin(UV.x * 11.0 + 0.8) * 0.035;
    soft_height += sin(UV.x * 23.0 + 2.1) * 0.025;
    soft_height += dynamic_length * 0.065;
    float minimum_length_scale = min(2.0 / max_length_multiplier, 1.0);
    float length_scale = mix(minimum_length_scale, 1.0, growth);
    soft_height *= length_scale;
    float top = 1.0 - soft_height;
    float vertical_fade = smoothstep(top, top + 0.36 * length_scale, UV.y);

    // Clip the light curtain against the lower half of the same ellipse used
    // by the ground grid. The center reaches the lowest point while both
    // sides rise along the arc, avoiding a straight horizontal cut.
    float ellipse_x = (UV.x - 0.5) * 2.0 * ellipse_width_ratio;
    float ellipse_arc = sqrt(max(1.0 - ellipse_x * ellipse_x, 0.0));
    float curved_bottom = 1.0 - ground_curve_ratio * (1.0 - ellipse_arc);
    float bottom_fade = 1.0 - smoothstep(curved_bottom - 0.085, curved_bottom, UV.y);
    vertical_fade *= bottom_fade;

    float edge_fade = smoothstep(0.0, 0.13, UV.x) * smoothstep(0.0, 0.13, 1.0 - UV.x);
    float breath = 0.84 + sin(TIME * breath_speed) * 0.16;
    float shimmer = 0.96 + sin(TIME * 0.72 + UV.x * 17.0) * 0.04;
    float activation = smoothstep(0.0, 1.0, active);
    float growth_brightness = mix(1.0, max_brightness_multiplier, growth);
    float light = streak_light * vertical_fade * edge_fade * breath * shimmer * activation * growth_brightness;

    COLOR = vec4(light_color.rgb * light, light * opacity);
}

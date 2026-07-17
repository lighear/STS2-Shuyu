shader_type canvas_item;
render_mode blend_add, unshaded;

uniform vec4 color_normal : source_color = vec4(0.60, 0.64, 0.67, 1.0);
uniform vec4 color_active : source_color = vec4(0.34, 0.70, 1.0, 1.0);
uniform float active : hint_range(0.0, 1.0) = 0.0;

uniform float hex_count : hint_range(2.0, 16.0) = 3.25;
uniform float edge_width : hint_range(0.01, 0.18) = 0.055;
uniform float breath_speed : hint_range(0.0, 4.0) = 1.15;
uniform float glow_intensity : hint_range(0.0, 3.0) = 1.35;
uniform float opacity : hint_range(0.0, 1.0) = 0.58;
uniform float falloff_start : hint_range(0.2, 0.9) = 0.62;

vec2 nearest_hex_center(vec2 point) {
    vec2 cell_size = vec2(1.0, 1.7320508);
    vec2 half_cell = cell_size * 0.5;
    vec2 center_a = mod(point, cell_size) - half_cell;
    vec2 center_b = mod(point - half_cell, cell_size) - half_cell;
    return dot(center_a, center_a) < dot(center_b, center_b) ? center_a : center_b;
}

float hex_border_distance(vec2 point) {
    vec2 absolute_point = abs(point);
    return 0.5 - max(absolute_point.x, absolute_point.x * 0.5 + absolute_point.y * 0.8660254);
}

void fragment() {
    // The ColorRect is physically flattened by C#, so this circular UV mask
    // becomes a perspective ellipse on the battlefield floor.
    vec2 floor_uv = (UV - vec2(0.5)) * 2.0;
    float floor_radius = length(floor_uv);

    // Slightly denser rows preserve readable projected hexagons in the very
    // wide ground rectangle without making them look like a vertical shield.
    vec2 grid_uv = vec2(floor_uv.x, floor_uv.y * 0.72) * hex_count;
    vec2 hex_point = nearest_hex_center(grid_uv);
    float border_distance = hex_border_distance(hex_point);
    float antialias = max(fwidth(border_distance), 0.002);
    float fine_line = 1.0 - smoothstep(edge_width, edge_width + antialias, border_distance);
    float soft_line = 1.0 - smoothstep(edge_width * 3.6, edge_width * 3.6 + antialias * 2.0, border_distance);

    float edge_fade = 1.0 - smoothstep(falloff_start, 1.0, floor_radius);
    float depth_fade = mix(0.72, 1.0, smoothstep(-1.0, 0.35, floor_uv.y));
    float breath = 0.82 + sin(TIME * breath_speed) * 0.18;

    // When activated, a single bright band travels from the feet to the rim.
    // At active=1 it has left the ellipse, returning to a calm persistent grid.
    float wave_radius = active * 1.16;
    float activation_wave = exp(-pow((floor_radius - wave_radius) / 0.075, 2.0));
    activation_wave *= sin(active * 3.14159265);
    float activated_cells = (1.0 - smoothstep(wave_radius - 0.04, wave_radius + 0.16, floor_radius)) * active;

    float activation_state = smoothstep(0.0, 1.0, active);
    float state_brightness = mix(0.333, 1.0, activation_state);
    float line_light = fine_line * breath * state_brightness;
    float glow_light = soft_line * mix(0.073, 0.22, activation_state) * breath;
    float activation_light = (fine_line * 1.5 + soft_line * 0.65) * activation_wave;
    float persistent_light = fine_line * activated_cells * 0.12;
    float light = (line_light + glow_light + activation_light + persistent_light) * edge_fade * depth_fade;

    vec3 grid_color = mix(color_normal.rgb, color_active.rgb, activation_state);
    grid_color = mix(grid_color, vec3(1.0), activation_wave * 0.42);
    float alpha = light * opacity;
    COLOR = vec4(grid_color * light * glow_intensity, alpha);
}

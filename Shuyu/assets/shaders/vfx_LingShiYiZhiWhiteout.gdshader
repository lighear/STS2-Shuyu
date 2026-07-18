shader_type canvas_item;
render_mode blend_mix, unshaded;

uniform vec4 tint : source_color = vec4(0.94, 0.98, 1.0, 1.0);
uniform float strength : hint_range(0.0, 1.0) = 0.0;
uniform float center_alpha : hint_range(0.0, 1.0) = 0.10;
uniform float edge_alpha : hint_range(0.0, 1.0) = 0.60;

void fragment() {
    // The central 60% of both axes begins at 20% from each viewport edge.
    // A broad band outside that rectangle blends smoothly back to the 60% rim.
    vec2 distance_to_edge = min(UV, vec2(1.0) - UV);
    float nearest_edge = min(distance_to_edge.x, distance_to_edge.y);
    float edge_weight = 1.0 - smoothstep(0.075, 0.20, nearest_edge);
    float local_alpha = mix(center_alpha, edge_alpha, edge_weight);
    COLOR = vec4(tint.rgb, local_alpha * strength);
}

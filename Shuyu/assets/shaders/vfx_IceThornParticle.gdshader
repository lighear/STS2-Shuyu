shader_type canvas_item;
render_mode blend_add, unshaded;

void fragment() {
    vec4 particle_color = COLOR;
    vec4 tex = texture(TEXTURE, UV);

    // The upper third becomes progressively whiter. Near the two sloping
    // edges the mask is stronger, giving the thorn a crisp cutting tip while
    // leaving the translucent blue body untouched.
    float tip_mask = 1.0 - smoothstep(0.03, 0.34, UV.y);
    float expected_half_width = mix(0.025, 0.46, UV.y);
    float lateral = abs(UV.x - 0.5);
    float edge_mask = smoothstep(expected_half_width * 0.55, expected_half_width, lateral);
    float sharp_mask = tip_mask * mix(0.62, 1.0, edge_mask);

    vec3 tinted_rgb = tex.rgb * particle_color.rgb;
    vec3 final_rgb = mix(tinted_rgb, vec3(1.0), sharp_mask * 0.88);
    float base_alpha = tex.a * particle_color.a;
    float sharp_alpha = min(1.0, base_alpha * 1.45 + tex.a * 0.16);
    float final_alpha = mix(base_alpha, sharp_alpha, sharp_mask);

    COLOR = vec4(final_rgb, final_alpha);
}

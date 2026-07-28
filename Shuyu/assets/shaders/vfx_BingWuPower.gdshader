shader_type canvas_item;

uniform sampler2D noise_texture : source_color, filter_linear_mipmap, repeat_enable;
uniform vec2 scroll_speed = vec2(0.05, 0.02);
uniform float noise_scale = 2.0;
uniform float aspect_ratio = 1.0;

uniform vec4 fog_color : source_color = vec4(1.0);
uniform float fog_density = 0.8;
uniform float noise_threshold = 0.35;
uniform float noise_softness = 0.25;

uniform float edge_fade_width = 0.25;
uniform float disturbance_strength = 0.03;
uniform float disturbance_frequency = 0.8;

void fragment() {
    float fade_left = smoothstep(0.0, edge_fade_width, UV.x);
    float fade_right = smoothstep(1.0, 1.0 - edge_fade_width, UV.x);
    float fade_top = smoothstep(0.0, edge_fade_width, UV.y);
    float fade_bottom = smoothstep(1.0, 1.0 - edge_fade_width, UV.y);
    float edge_fade = fade_left * fade_right * fade_top * fade_bottom;

    // Noise coordinates are measured in units of the rectangle width so fog
    // patches stay round instead of stretching with a tall character layout.
    vec2 noise_uv = vec2(
        UV.x,
        (UV.y - 0.5) * aspect_ratio + 0.5
    );

    float dist_phase =
        noise_uv.x * 3.5
        + noise_uv.y * 2.7
        + TIME * 0.2;
    vec2 disturbance = vec2(
        sin(dist_phase * disturbance_frequency)
            * cos(dist_phase * 1.3 + 1.2)
            * disturbance_strength,
        cos(dist_phase * disturbance_frequency * 0.9 + 2.4)
            * sin(dist_phase * 1.7)
            * disturbance_strength
    );

    vec2 scrolled_uv1 =
        noise_uv * noise_scale
        + TIME * scroll_speed * 0.1
        + disturbance;
    float noise1 = texture(noise_texture, scrolled_uv1).r;

    vec2 scrolled_uv2 =
        noise_uv * (noise_scale * 2.5)
        + TIME * scroll_speed * 0.15
        + vec2(0.5)
        + disturbance * 1.2;
    float noise2 = texture(noise_texture, scrolled_uv2).r;

    float final_noise = mix(noise1, noise2, 0.4);
    final_noise = smoothstep(
        noise_threshold,
        noise_threshold + noise_softness,
        final_noise
    );

    float alpha = final_noise * edge_fade * fog_density;
    COLOR = vec4(fog_color.rgb, alpha);
}

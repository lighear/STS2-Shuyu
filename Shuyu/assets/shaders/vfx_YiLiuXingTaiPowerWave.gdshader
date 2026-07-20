shader_type canvas_item;
render_mode blend_mix, unshaded;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear;
uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform vec4 line_color : source_color = vec4(0.55, 0.9, 1.0, 1.0);
uniform float distortion_strength : hint_range(0.0, 0.02, 0.0005) = 0.005;
uniform float panel_side_pixels = 720.0;
uniform float start_radius = 0.055;

mat2 rotate_2d(float angle) {
	float sine = sin(angle);
	float cosine = cos(angle);
	return mat2(vec2(cosine, -sine), vec2(sine, cosine));
}

void fragment() {
	vec2 centered_uv = UV - vec2(0.5);
	float angle = mix(-0.32, 0.66, progress);
	vec2 square_uv = rotate_2d(angle) * centered_uv;

	float eased_progress = progress * progress;
	float square_radius = mix(start_radius, 0.49, eased_progress);
	float square_distance = max(abs(square_uv.x), abs(square_uv.y));
	float distance_to_line = abs(square_distance - square_radius);

	// Convert the original 720 px tuning to UV units so enlarging the panel does
	// not make the line, glow, or distortion band visibly thicker.
	float pixel_to_uv = 720.0 / max(panel_side_pixels, 1.0);
	float line = 1.0 - smoothstep(0.0025 * pixel_to_uv, 0.0105 * pixel_to_uv, distance_to_line);
	float glow = 1.0 - smoothstep(0.006 * pixel_to_uv, 0.042 * pixel_to_uv, distance_to_line);
	float distortion_band = 1.0 - smoothstep(0.012 * pixel_to_uv, 0.055 * pixel_to_uv, distance_to_line);
	float fade_in = smoothstep(0.0, 0.08, progress);
	float fade_out = 1.0 - smoothstep(0.88, 1.0, progress);
	float lifetime = fade_in * fade_out;

	vec2 square_normal;
	if (abs(square_uv.x) > abs(square_uv.y)) {
		square_normal = vec2(sign(square_uv.x), 0.0);
	} else {
		square_normal = vec2(0.0, sign(square_uv.y));
	}
	square_normal = rotate_2d(-angle) * square_normal;
	float ripple_noise = 0.72 + 0.28 * sin((square_uv.x + square_uv.y) * 76.0 - progress * 15.0);
	vec2 distortion_offset = square_normal * distortion_strength * distortion_band * lifetime * ripple_noise;

	vec3 distorted_screen = texture(screen_texture, SCREEN_UV - distortion_offset).rgb;
	float blue_mix = clamp((line * 0.88 + glow * 0.24) * lifetime, 0.0, 0.92);
	vec3 result = mix(distorted_screen, line_color.rgb, blue_mix);
	float alpha = clamp(max(distortion_band * 0.55, line + glow * 0.22) * lifetime, 0.0, 1.0);

	COLOR = vec4(result, alpha);
}

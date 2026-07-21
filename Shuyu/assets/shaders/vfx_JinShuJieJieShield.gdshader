shader_type canvas_item;
render_mode blend_mix, unshaded;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform vec4 deep_color : source_color = vec4(0.006, 0.005, 0.012, 1.0);
uniform vec4 cold_color : source_color = vec4(0.055, 0.037, 0.085, 1.0);

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float noise(vec2 p) {
	vec2 cell = floor(p);
	vec2 local = fract(p);
	local = local * local * (3.0 - 2.0 * local);
	float a = hash(cell);
	float b = hash(cell + vec2(1.0, 0.0));
	float c = hash(cell + vec2(0.0, 1.0));
	float d = hash(cell + vec2(1.0, 1.0));
	return mix(mix(a, b, local.x), mix(c, d, local.x), local.y);
}

float fbm(vec2 p) {
	float value = 0.0;
	float weight = 0.55;
	for (int i = 0; i < 5; i++) {
		value += noise(p) * weight;
		p = p * 2.04 + vec2(6.7, 3.1);
		weight *= 0.5;
	}
	return value;
}

void fragment() {
	vec2 p = (UV - vec2(0.5)) * 2.0;
	p.y += 0.035;

	float gather = 1.0 - pow(1.0 - clamp(progress / 0.68, 0.0, 1.0), 2.2);
	float fade_in = smoothstep(0.0, 0.10, progress);
	float fade_out = 1.0 - smoothstep(0.70, 1.0, progress);
	float lifetime = fade_in * fade_out;

	// Narrow the lower half so the condensed ring reads as a protective shield
	// rather than a perfectly regular oval.
	float lower_taper = 1.0 + max(p.y, 0.0) * 0.30;
	vec2 shield_p = vec2(p.x * lower_taper, p.y * 0.92);
	float radial_distance = length(shield_p);
	float radius = mix(0.92, 0.55, gather);
	float thickness = mix(0.34, 0.075, gather);

	vec2 flow = p * mix(4.1, 7.3, gather);
	float broad_noise = fbm(flow + vec2(TIME * 0.18, -TIME * 0.12));
	float detail_noise = fbm(flow * 2.15 - vec2(TIME * 0.11, TIME * 0.17));
	float warped_radius = radius
		+ (broad_noise - 0.5) * thickness * 1.25
		+ (detail_noise - 0.5) * thickness * 0.34;
	float distance_to_shell = abs(radial_distance - warped_radius);

	float dense_shell = 1.0 - smoothstep(thickness * 0.16, thickness * 0.92, distance_to_shell);
	float soft_fog = 1.0 - smoothstep(thickness * 0.34, thickness * 2.25, distance_to_shell);
	float drifting_wisps = 1.0 - smoothstep(thickness * 0.65, thickness * 3.0, distance_to_shell);
	drifting_wisps *= smoothstep(0.48, 0.82, broad_noise + detail_noise * 0.20);

	float condensation = smoothstep(0.18, 0.62, progress);
	float alpha = (dense_shell * mix(0.34, 0.76, condensation)
		+ soft_fog * 0.28
		+ drifting_wisps * mix(0.34, 0.12, condensation)) * lifetime;

	// The local fog canvas is deliberately larger than the creature. Fade its
	// outermost pixels in UV space so no rectangular edge can ever be exposed.
	vec2 edge_distance = min(UV, vec2(1.0) - UV);
	float boundary_fade = smoothstep(0.0, 0.16, edge_distance.x)
		* smoothstep(0.0, 0.16, edge_distance.y);
	alpha *= boundary_fade;
	alpha = clamp(alpha, 0.0, 0.86);

	vec3 fog_color = mix(deep_color.rgb, cold_color.rgb, broad_noise * 0.34);
	COLOR = vec4(fog_color, alpha);
}

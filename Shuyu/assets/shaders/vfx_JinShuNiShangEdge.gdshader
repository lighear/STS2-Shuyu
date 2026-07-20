shader_type canvas_item;
render_mode blend_mix, unshaded;

uniform float strength : hint_range(0.0, 1.0) = 0.0;
uniform vec4 edge_color : source_color = vec4(0.012, 0.008, 0.027, 1.0);

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
	for (int i = 0; i < 4; i++) {
		value += noise(p) * weight;
		p = p * 2.03 + vec2(7.4, 3.8);
		weight *= 0.5;
	}
	return value;
}

void fragment() {
	vec2 distance_to_edge = min(UV, vec2(1.0) - UV);
	float nearest_edge = min(distance_to_edge.x, distance_to_edge.y);
	float vignette = 1.0 - smoothstep(0.025, 0.30, nearest_edge);
	float fog_noise = fbm(UV * vec2(5.5, 4.0) + vec2(TIME * 0.07, -TIME * 0.045));
	float uneven_edge = mix(0.68, 1.0, fog_noise);
	float alpha = vignette * uneven_edge * strength * 0.82;
	vec3 tint = mix(edge_color.rgb, vec3(0.045, 0.027, 0.085), fog_noise * 0.22);
	COLOR = vec4(tint, alpha);
}

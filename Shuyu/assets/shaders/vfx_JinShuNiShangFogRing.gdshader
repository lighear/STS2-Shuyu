shader_type canvas_item;
render_mode blend_mix, unshaded;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear;
uniform vec2 center_uv = vec2(0.5);
uniform float aspect_ratio = 1.7777778;
uniform float start_radius = 1.4;
uniform float end_radius = 0.1;
uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float opacity : hint_range(0.0, 1.0) = 0.0;
uniform float distortion_strength : hint_range(0.0, 0.015, 0.0005) = 0.004;

vec2 random_gradient(vec2 p) {
	vec2 seed = vec2(
		dot(p, vec2(127.1, 311.7)),
		dot(p, vec2(269.5, 183.3))
	);
	return -1.0 + 2.0 * fract(sin(seed) * 43758.5453);
}

float noise(vec2 p) {
	vec2 cell = floor(p);
	vec2 local = fract(p);
	vec2 smooth_local = local * local * local * (local * (local * 6.0 - 15.0) + 10.0);
	float a = dot(random_gradient(cell), local);
	float b = dot(random_gradient(cell + vec2(1.0, 0.0)), local - vec2(1.0, 0.0));
	float c = dot(random_gradient(cell + vec2(0.0, 1.0)), local - vec2(0.0, 1.0));
	float d = dot(random_gradient(cell + vec2(1.0, 1.0)), local - vec2(1.0, 1.0));
	float gradient_noise = mix(mix(a, b, smooth_local.x), mix(c, d, smooth_local.x), smooth_local.y);
	return clamp(gradient_noise * 0.72 + 0.5, 0.0, 1.0);
}

float fbm(vec2 p) {
	float value = 0.0;
	float weight = 0.55;
	for (int i = 0; i < 5; i++) {
		value += noise(p) * weight;
		p = p * 2.07 + vec2(5.7, 9.2);
		weight *= 0.5;
	}
	return value;
}

mat2 rotate_2d(float angle) {
	float sine = sin(angle);
	float cosine = cos(angle);
	return mat2(vec2(cosine, -sine), vec2(sine, cosine));
}

void fragment() {
	vec2 screen_delta = UV - center_uv;
	vec2 distortion_direction = normalize(screen_delta + vec2(0.00001));
	vec2 p = screen_delta;
	p.x *= aspect_ratio;

	float radial_distance = length(p);
	float polar_angle = atan(p.y, p.x);
	float density = 0.0;
	float distortion_weight = 0.0;
	float fog_tone = 0.0;
	float fog_tone_weight = 0.0;

	// Four independently timed rings travel in succession. Their individual
	// travel time is shorter than the overall effect, giving the requested
	// faster motion while retaining a 1.5 second four-ring sequence.
	for (int ring_index = 0; ring_index < 4; ring_index++) {
		float index = float(ring_index);
		float raw_ring_progress = progress * 1.32 - index * 0.105;
		float ring_progress = clamp(raw_ring_progress, 0.0, 1.0);
		float active = step(0.0, raw_ring_progress) * step(raw_ring_progress, 1.0);

		// Ease out: the ring crosses the outer screen quickly, then loses speed
		// as it closes in on the character.
		float eased_progress = 1.0 - pow(1.0 - ring_progress, 1.75);
		float radius = mix(start_radius, end_radius, eased_progress);
		float thickness_scale = 1.0 + 0.11 * sin(index * 2.3 + 0.4);
		float thickness = mix(0.19, 0.085, eased_progress) / 3.0 * thickness_scale;

		// Give each ring its own rotating fog field. Adjacent rings rotate in
		// opposite directions with slightly different angular velocities.
		float rotation_direction = mod(index, 2.0) < 1.0 ? 1.0 : -1.0;
		float rotation_angle = rotation_direction
			* (TIME * (0.16 + index * 0.035) + progress * (0.52 + index * 0.09))
			+ index * 1.37;
		vec2 texture_p = rotate_2d(rotation_angle) * p;
		vec2 flow_uv = texture_p * 4.8;
		vec2 texture_seed = vec2(index * 6.31, index * -4.77);
		vec2 domain_warp = vec2(
			fbm(flow_uv + texture_seed + vec2(TIME * 0.07, -TIME * 0.05)),
			fbm(flow_uv + texture_seed + vec2(13.7, 8.2) - vec2(TIME * 0.045, TIME * 0.065))
		) - vec2(0.5);
		float broad_noise = fbm(texture_p * 6.8 + texture_seed + domain_warp * 1.35);
		float detail_noise = fbm(texture_p * 16.0 - texture_seed * 0.43 + domain_warp * 2.3);
		float texture_angle = polar_angle - rotation_angle;

		float ring_variation = sin(texture_angle * (5.0 + index) + index * 1.73);
		float warped_radius = radius
			+ (broad_noise - 0.5) * thickness * 1.05
			+ (detail_noise - 0.5) * thickness * 0.30
			+ ring_variation * thickness * 0.22;
		float distance_to_ring = abs(radial_distance - warped_radius);

		float dense_core = 1.0 - smoothstep(thickness * 0.16, thickness * 0.92, distance_to_ring);
		float soft_body = 1.0 - smoothstep(thickness * 0.30, thickness * 1.70, distance_to_ring);
		float wisps = 1.0 - smoothstep(thickness * 0.66, thickness * 2.55, distance_to_ring);
		wisps *= smoothstep(0.24, 0.76, detail_noise + broad_noise * 0.44);

		float fade_in = smoothstep(0.0, 0.07, ring_progress);
		float approach_fade = 1.0 - smoothstep(0.72, 0.97, ring_progress);
		float ring_lifetime = active * fade_in * approach_fade;
		float ring_density = dense_core * 0.70 + soft_body * 0.38 + wisps * 0.28;
		float ring_contribution = ring_density * ring_lifetime;
		density += ring_contribution;
		fog_tone += (broad_noise * 0.42 + detail_noise * 0.12) * ring_contribution;
		fog_tone_weight += ring_contribution;
		float local_distortion = soft_body * ring_lifetime * mix(0.58, 1.0, detail_noise);
		distortion_weight += local_distortion;
	}

	float fog_alpha = clamp(density * opacity, 0.0, 0.88);
	distortion_weight = clamp(distortion_weight, 0.0, 1.0);
	float distortion_alpha = distortion_weight * opacity * 0.58;
	vec2 distortion_offset = distortion_direction
		* distortion_strength
		* distortion_weight
		* (0.72 + 0.28 * sin(polar_angle * 11.0 - TIME * 2.4));
	vec3 distorted_screen = texture(screen_texture, SCREEN_UV - distortion_offset).rgb;

	vec3 deep_black = vec3(0.008, 0.006, 0.018);
	vec3 cold_violet = vec3(0.082, 0.052, 0.16);
	float averaged_fog_tone = fog_tone / max(fog_tone_weight, 0.0001);
	vec3 fog_color = mix(deep_black, cold_violet, clamp(averaged_fog_tone, 0.0, 0.54));
	float output_alpha = max(fog_alpha, distortion_alpha);
	float fog_mix = fog_alpha / max(output_alpha, 0.0001);
	vec3 result = mix(distorted_screen, fog_color, fog_mix);
	COLOR = vec4(result, output_alpha);
}

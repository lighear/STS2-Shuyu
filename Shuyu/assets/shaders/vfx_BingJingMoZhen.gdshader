shader_type canvas_item;
render_mode blend_add, unshaded;

uniform vec4 tint : source_color = vec4(1.0);
uniform float blur_radius : hint_range(0.0, 48.0, 1.0) = 0.0;
uniform float core_strength : hint_range(0.0, 1.0) = 1.0;
uniform float glow_strength : hint_range(0.0, 1.0) = 0.0;

void fragment() {
	vec2 pixel = 1.0 / vec2(textureSize(TEXTURE, 0));
	vec2 radius = pixel * blur_radius;
	float core = texture(TEXTURE, UV).a;
	float glow = core * 0.16;

	glow += texture(TEXTURE, UV + vec2(radius.x, 0.0)).a * 0.105;
	glow += texture(TEXTURE, UV - vec2(radius.x, 0.0)).a * 0.105;
	glow += texture(TEXTURE, UV + vec2(0.0, radius.y)).a * 0.105;
	glow += texture(TEXTURE, UV - vec2(0.0, radius.y)).a * 0.105;
	glow += texture(TEXTURE, UV + radius * 0.7).a * 0.105;
	glow += texture(TEXTURE, UV - radius * 0.7).a * 0.105;
	glow += texture(TEXTURE, UV + vec2(radius.x, -radius.y) * 0.7).a * 0.105;
	glow += texture(TEXTURE, UV + vec2(-radius.x, radius.y) * 0.7).a * 0.105;

	float alpha = clamp(core * core_strength + glow * glow_strength, 0.0, 1.0) * tint.a;
	COLOR = vec4(tint.rgb, alpha);
}

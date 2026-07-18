shader_type canvas_item;
render_mode blend_add, unshaded;

uniform vec4 tint : source_color = vec4(0.88, 0.97, 1.0, 0.8);
uniform float opacity : hint_range(0.0, 1.0) = 1.0;
uniform float glow_radius : hint_range(0.0, 12.0) = 0.0;

void fragment() {
    vec2 pixel = TEXTURE_PIXEL_SIZE * glow_radius;
    float center_alpha = texture(TEXTURE, UV).a;
    float spread_alpha = center_alpha;

    spread_alpha += texture(TEXTURE, UV + vec2(pixel.x, 0.0)).a;
    spread_alpha += texture(TEXTURE, UV - vec2(pixel.x, 0.0)).a;
    spread_alpha += texture(TEXTURE, UV + vec2(0.0, pixel.y)).a;
    spread_alpha += texture(TEXTURE, UV - vec2(0.0, pixel.y)).a;
    spread_alpha += texture(TEXTURE, UV + pixel).a;
    spread_alpha += texture(TEXTURE, UV - pixel).a;
    spread_alpha += texture(TEXTURE, UV + vec2(pixel.x, -pixel.y)).a;
    spread_alpha += texture(TEXTURE, UV + vec2(-pixel.x, pixel.y)).a;

    float blurred_alpha = spread_alpha / 9.0;
    float mask = mix(center_alpha, max(center_alpha, blurred_alpha), step(0.01, glow_radius));
    COLOR = vec4(tint.rgb, mask * tint.a * opacity);
}

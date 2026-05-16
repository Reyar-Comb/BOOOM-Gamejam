shader_type canvas_item;

uniform vec4 vignette_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform float inner_radius : hint_range(0.0, 1.0) = 0.35;
uniform float outer_radius : hint_range(0.0, 1.5) = 0.85;
uniform float intensity : hint_range(0.0, 1.0) = 0.65;

void fragment() {
	vec2 centered_uv = UV - vec2(0.5);
	float distance_from_center = length(centered_uv);
	float vignette = smoothstep(inner_radius, outer_radius, distance_from_center) * intensity;
	COLOR = vec4(vignette_color.rgb, vignette * vignette_color.a);
}

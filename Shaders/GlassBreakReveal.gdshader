shader_type canvas_item;
render_mode blend_mix, unshaded;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform vec2 reveal_center = vec2(0.5, 0.5);
uniform vec2 block_grid = vec2(32.0, 18.0);
uniform float block_fade : hint_range(0.005, 0.2) = 0.055;
uniform float random_delay : hint_range(0.0, 0.25) = 0.08;
uniform float edge_width : hint_range(0.0, 0.12) = 0.035;
uniform vec4 edge_color : source_color = vec4(0.85, 1.0, 1.0, 0.72);
uniform bool overlay_mode = false;
uniform bool cover_from_edges = false;
uniform vec4 overlay_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);

float hash12(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

void fragment() {
	vec2 uv = UV;
	vec2 grid = max(floor(block_grid), vec2(1.0));
	vec2 cell = floor(uv * grid);
	vec2 cell_uv = fract(uv * grid);
	vec2 cell_center = (cell + vec2(0.5)) / grid;

	vec2 center = clamp(reveal_center, vec2(0.0), vec2(1.0));
	vec2 max_corner_delta = max(center, vec2(1.0) - center);
	float max_distance = max(length(max_corner_delta), 0.0001);
	float center_distance = length(cell_center - center) / max_distance;

	float jitter = (hash12(cell) - 0.5) * random_delay;
	float transition_distance = cover_from_edges ? 1.0 - center_distance : center_distance;
	float transition_start = clamp(transition_distance + jitter, 0.001, 1.0);
	float transition_amount = smoothstep(transition_start, transition_start + block_fade, progress);

	vec4 tex = texture(TEXTURE, uv);

	vec2 edge_distance = min(cell_uv, vec2(1.0) - cell_uv);
	float cell_edge = 1.0 - smoothstep(0.0, edge_width, min(edge_distance.x, edge_distance.y));
	float active_edge = cell_edge * transition_amount * (1.0 - transition_amount) * edge_color.a;

	if (overlay_mode) {
		float overlay_alpha = cover_from_edges ? transition_amount : 1.0 - transition_amount;
		vec3 color = mix(overlay_color.rgb, edge_color.rgb, active_edge);
		COLOR = vec4(color, overlay_color.a * overlay_alpha);
	}
	else
	{
	vec3 color = mix(tex.rgb, edge_color.rgb, active_edge);
	float alpha = tex.a * (1.0 - transition_amount);
	COLOR = vec4(color, alpha);
	}
}

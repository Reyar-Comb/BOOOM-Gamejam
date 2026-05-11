shader_type canvas_item;
render_mode blend_mix;

// ==============================
// BlinkingProgressBar shader
// 单节点进度条，支持闪烁的"扣费预览"段
// ==============================

// 当前进度 (0.0 ~ 1.0)
uniform float progress : hint_range(0.0, 1.0) = 0.5;
// 扣费后的目标进度 (0.0 ~ 1.0)，与 progress 之间的差值段会闪烁
uniform float target : hint_range(0.0, 1.0) = 0.5;

// 背景色
uniform vec4 bg_color : source_color = vec4(0.6, 0.6, 0.6, 0.38);
// 基础填充色（当前余量）
uniform vec4 base_color : source_color = vec4(0.0, 1.0, 0.0, 0.56);
// 闪烁段颜色（半透明）
uniform vec4 ghost_color : source_color = vec4(1.0, 0.3, 0.3, 0.6);
// 扣费用完时的警告色（target = 0 时整个剩余进度条闪烁此色）
uniform vec4 empty_color : source_color = vec4(1.0, 0.1, 0.1, 0.7);
// 边框色
uniform vec4 border_color : source_color = vec4(1.0, 1.0, 1.0, 0.0);

// 边框宽度（像素）
uniform float border_width : hint_range(0.0, 200.0) = 2.0;
// 圆角半径（像素）
uniform float corner_radius : hint_range(0.0, 200.0) = 5.0;
// ColorRect 的像素尺寸（在脚本中传入 rect_size）
uniform vec2 node_size = vec2(46, 822);

// 闪烁速度
uniform float blink_speed : hint_range(0.1, 20.0) = 3.0;
// 闪烁段的最小透明度（闪烁范围：ghost_min_alpha ~ 1.0）
uniform float ghost_min_alpha : hint_range(0.0, 1.0) = 0.1;


// ---- 圆角矩形 SDF ----
float rounded_box_sdf(vec2 p, vec2 half_size, float r)
{
	vec2 d = abs(p) - half_size + r;
	return min(max(d.x, d.y), 0.0) + length(max(d, 0.0)) - r;
}


void fragment()
{
	// 转到像素坐标系：原点在中心，单位 = 像素
	vec2 p = (UV - 0.5) * node_size;
	vec2 half_size = node_size * 0.5;

	// 外轮廓 SDF（像素单位）
	float shape_dist = rounded_box_sdf(p, half_size, corner_radius);

	// 内部（扣除边框后）SDF（像素单位）
	float inner_r = max(corner_radius - border_width, 0.0);
	vec2 inner_half = half_size - border_width;
	float inner_dist = rounded_box_sdf(p, inner_half, inner_r);

	// ---- 整体遮罩 ----
	float shape_alpha = 1.0 - smoothstep(0.0, 0.002, shape_dist);
	vec4 color = vec4(0.0);

	if (shape_alpha >= 0.005)
	{
		// ---- 边框区域判断 ----
		float inner_alpha = 1.0 - smoothstep(0.0, 0.002, inner_dist);
		bool is_border = inner_alpha < 0.5;

		// ---- 内容区域 ----
		color = bg_color;

		// 基础填充（从下往上）
		float fill = step(1.0 - UV.y, progress);
		if (fill > 0.5)
		{
			if (target <= 0.0)
			{
				// 扣费用完模式：整段红色闪烁
				float blink = 0.5 + 0.5 * sin(TIME * blink_speed * 3.14159);
				float blink_a = mix(ghost_min_alpha, 1.0, blink);
				color = mix(vec4(empty_color.rgb, empty_color.a * ghost_min_alpha), empty_color, blink_a);
			}
			else
			{
				color = base_color;
			}
		}

		// 闪烁段（progress 与 target 之间的差值段，从下往上）
		float ghost_start = min(progress, target);
		float ghost_end = max(progress, target);
		float in_ghost = step(1.0 - UV.y, ghost_end) - step(1.0 - UV.y, ghost_start);
		if (in_ghost > 0.5)
		{
			float blink = 0.5 + 0.5 * sin(TIME * blink_speed * 3.14159);
			float blink_a = mix(ghost_min_alpha, 1.0, blink);
			color = mix(color, ghost_color, blink_a);
		}

		// 边框覆盖
		if (is_border)
			color = border_color;

		color.a *= shape_alpha;
	}

	COLOR = color;
}

shader_type canvas_item;

// 雷达扫描线滤镜 —— 挂载于覆盖全屏的 ColorRect 上
// 采样底层游戏画面，叠加像素点阵 + 周期性从上到下的扫描亮线及拖尾

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;

// ========== 扫描控制 ==========
uniform float speed : hint_range(0.05, 2.0) = 0.5;
// ^^ 扫描线移动速度（UV 单位/秒），默认 0.5 即约 2 秒扫完全屏

uniform float interval_time : hint_range(0.0, 10.0) = 3.0;
// ^^ 两次扫描开始的间隔（秒），设为 0 则连续扫描

uniform float trail_length : hint_range(0.05, 1.0) = 0.3;
// ^^ 拖尾长度（UV 单位，0=顶部，1=底部）

uniform float line_brightness : hint_range(0.0, 8.0) = 2.5;
// ^^ 扫描线头部峰值亮度倍率

// ========== 点阵外观 ==========
uniform vec4 dot_color : source_color = vec4(0.0, 0.9, 0.4, 1.0);
// ^^ 点阵颜色

uniform float dot_density : hint_range(10.0, 200.0) = 80.0;
// ^^ 点阵密度（每单位 UV 的点数，即纵向点数）

uniform float dot_size : hint_range(0.05, 0.8) = 0.35;
// ^^ 点的大小（0=针尖，0.8=几乎填满格子）

uniform float base_alpha : hint_range(0.0, 0.3) = 0.04;
// ^^ 空闲时点阵的微弱可见度

uniform float global_alpha : hint_range(0.0, 1.0) = 1.0;
// ^^ 所有点的整体透明度

void fragment() {
	// 1. 采样底层游戏画面
	vec4 screen = texture(screen_texture, SCREEN_UV);

	// 2. 像素点阵
	vec2 grid_uv = fract(UV * dot_density);
	vec2 delta = abs(grid_uv - 0.5) * 2.0;
	float dot_dist = max(delta.x, delta.y); // Chebyshev → 方形点；0=中心，1=边缘
	float dot_mask = 1.0 - smoothstep(dot_size - 0.08, dot_size + 0.08, dot_dist);

	// 3. 扫描线位置（0=顶部，1+trail_length=已完全离开底部）
	//    扫描头以 speed（UV/s）移动，每到 interval_time 秒后从顶部重新开始
	float t = mod(TIME, max(interval_time, 0.001));
	float scan_head = t * speed;

	// 4. 当前像素相对于扫描头的距离
	//    dy > 0 → 在扫描头后方（已扫过的区域，应有亮度衰减）
	//    dy < 0 → 在扫描头前方（尚未扫描）
	float dy = scan_head - UV.y;

	// 5. 扫描亮度：指数衰减，扫描头最亮，越远越暗
	float scan_brightness = 0.0;
	if (dy > 0.0) {
		scan_brightness = line_brightness * exp(-dy * 5.0 / trail_length);
	}

	// 6. 合成点阵 alpha = 基底微弱可见 + 扫描增亮，再乘以全局透明度
	float dot_alpha = dot_mask * (base_alpha + scan_brightness) * global_alpha;
	dot_alpha = clamp(dot_alpha, 0.0, 1.0);

	// 7. 叠加到游戏画面上
	COLOR.rgb = mix(screen.rgb, dot_color.rgb, dot_alpha);
	COLOR.a = 1.0;
}

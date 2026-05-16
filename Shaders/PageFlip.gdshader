shader_type canvas_item;
render_mode blend_mix, unshaded;

// =====================================================
// PageFlip.gdshader — 黑屏淡入淡出过渡
// progress 0.0 → 0.5: 屏幕渐黑（遮住当前页）
// progress 0.5 → 1.0: 屏幕渐亮（露出下一页）
// PPTScene 在 progress=0.5 时切换幻灯片
// =====================================================

uniform float progress : hint_range(0.0, 1.0) = 0.0;

void fragment() {
    // 三角波：progress 0→0.5→1 映射 alpha 0→1→0
    float alpha = 1.0 - abs(progress * 2.0 - 1.0);
    COLOR = vec4(0.0, 0.0, 0.0, alpha);
}

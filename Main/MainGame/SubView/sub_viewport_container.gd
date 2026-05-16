extends SubViewportContainer

func _process(_delta):
	if material == null:
		return

	# 获取鼠标在当前节点内的局部坐标 (像素)
	var m_pos = get_local_mouse_position()
	# 获取节点的尺寸
	var t_size = size
	
	# 将鼠标坐标归一化到 0-1 范围
	material.set_shader_parameter("mouse_pos", m_pos / t_size)
	
	# 将 SubViewport 的渲染纹理传给 Shader
	var sub_viewport = $SubViewport
	if sub_viewport:
		material.set_shader_parameter("viewport_texture", sub_viewport.get_texture())

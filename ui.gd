extends Control


func _on_texture_button_pressed() -> void:
	print("Button Pressed")
	pass # Replace with function body.


func _on_texture_button_gui_input(event:InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
		print("Mouse Button here")
	pass # Replace with function body.


func _on_texture_button_mouse_entered() -> void:
	print("Mouse Entered Button")
	pass # Replace with function body.

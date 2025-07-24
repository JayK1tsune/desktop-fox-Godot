extends AnimatedSprite2D


@onready var area: Area2D = $ClickLogic
@onready var collision_shape : CollisionShape2D = $ClickLogic/CollisionShape2D


func _ready():
	pass

func _process(_delta):
	var mouspos = get_viewport().get_mouse_position()

	print(mouspos)
	print(collision_shape.position)

	if mouspos == collision_shape.get_local_mouse_position():
		print("Hello")

	pass

using Godot;
using System;

public class Camera : Godot.Camera
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    float sensitivity = 0.25f;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {

    }

    private Vector3 _direction = new Vector3(0.0f, 0.0f, 0.0f);

    private Vector3 _velocity = new Vector3(0.0f, 0.0f, 0.0f);
    float _acceleration = 120;
    float _deceleration = -10;
    float _vel_multiplier = 4;


    // # Mouse state
    Vector2 _mouse_position = new Vector2(0.0f, 0.0f);
    float _total_pitch = 0.0f;

    private bool pressed_w;
    private bool pressed_a;
    private bool pressed_s;
    private bool pressed_d;
    private bool pressed_ctrl; 

    public override void _Input(InputEvent e)
    {
        base._Input(e);

        if (e.GetType() == typeof(InputEventMouseMotion))
        {
            _mouse_position = ((InputEventMouseMotion)e).Relative;
        }


        if (e.GetType() == typeof(InputEventMouseButton))
        {
            switch (((InputEventMouseButton)e).ButtonIndex)
            {
                case (int)ButtonList.Right:
                    Input.MouseMode = e.IsPressed() ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
                    break;
                case (int)ButtonList.WheelUp:
                    _vel_multiplier = Mathf.Clamp(_vel_multiplier * 1.1f, 0.2f, 20f);
                    break;
                case (int)ButtonList.WheelDown:
                    _vel_multiplier = Mathf.Clamp(_vel_multiplier / 1.1f, 0.2f, 20f);
                    break;
                default:
                    break;
            }
        }

        if (e.GetType() == typeof(InputEventKey))
        {
            InputEventKey iek = (InputEventKey)e;
            switch (iek.Scancode)
            {
                case (uint)KeyList.W: pressed_w = iek.Pressed; break;
                case (uint)KeyList.S: pressed_s = iek.Pressed; break;
                case (uint)KeyList.A: pressed_a = iek.Pressed; break;
                case (uint)KeyList.D: pressed_d = iek.Pressed; break;
                case (uint)KeyList.Control: pressed_ctrl = iek.Pressed; break;
                default:
                    break;
            }
        }       
    }

    public void UpdateMouseLook()
    {
        if (Input.MouseMode == Input.MouseModeEnum.Captured)
        {
		    _mouse_position *= sensitivity;
		    float yaw = _mouse_position.x;
    		float pitch = _mouse_position.y;
		    _mouse_position = new Vector2(0, 0);
            RotateY(Mathf.Deg2Rad(-yaw));
            RotateObjectLocal(new Vector3(1,0,0), Mathf.Deg2Rad(-pitch));
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        UpdateMouseLook();

        // increase speed if pressed left control
        _vel_multiplier = pressed_ctrl ? _acceleration : 4;

        _direction = new Vector3(Convert.ToSingle(pressed_d) - Convert.ToSingle(pressed_a), 0f, Convert.ToSingle(pressed_s) - Convert.ToSingle(pressed_w));

        Vector3 offset = _direction.Normalized() * _acceleration * _vel_multiplier * delta + _velocity.Normalized() * _deceleration * _vel_multiplier * delta;

        if (_direction == Vector3.Zero && offset.LengthSquared() > _velocity.LengthSquared())
        {
            _velocity = Vector3.Zero;
        }
        else
        {
            _velocity.x = Mathf.Clamp(_velocity.x + offset.x, -_vel_multiplier, _vel_multiplier);
            _velocity.y = Mathf.Clamp(_velocity.y + offset.y, -_vel_multiplier, _vel_multiplier);
            _velocity.z = Mathf.Clamp(_velocity.z + offset.z, -_vel_multiplier, _vel_multiplier);
        }

        Translate(_velocity * delta);
        // 	# Computes the change in velocity due to desired direction and "drag"
        // 	# The "drag" is a constant acceleration on the camera to bring it's velocity to 0
        // 	var offset = _direction.normalized() * _acceleration * _vel_multiplier * delta \
        // 		+ _velocity.normalized() * _deceleration * _vel_multiplier * delta

        // 	# Checks if we should bother translating the camera
        // 	if _direction == Vector3.ZERO and offset.length_squared() > _velocity.length_squared():
        // 		# Sets the velocity to 0 to prevent jittering due to imperfect deceleration
        // 		_velocity = Vector3.ZERO
        // 	else:
        // 		# Clamps speed to stay within maximum value (_vel_multiplier)
        // 		_velocity.x = clamp(_velocity.x + offset.x, -_vel_multiplier, _vel_multiplier)
        // 		_velocity.y = clamp(_velocity.y + offset.y, -_vel_multiplier, _vel_multiplier)
        // 		_velocity.z = clamp(_velocity.z + offset.z, -_vel_multiplier, _vel_multiplier)

        // 		translate(_velocity * delta)

// 		_mouse_position *= sensitivity
// 		var yaw = _mouse_position.x
// 		var pitch = _mouse_position.y
// 		_mouse_position = Vector2(0, 0)

// 		# Prevents looking up/down too far
// 		pitch = clamp(pitch, -90 - _total_pitch, 90 - _total_pitch)
// 		_total_pitch += pitch

// 		rotate_y(deg2rad(-yaw))
// 		rotate_object_local(Vector3(1,0,0), deg2rad(-pitch))
    }
}

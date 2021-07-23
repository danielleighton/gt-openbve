shader_type spatial; 
render_mode cull_disabled;

uniform sampler2D day_texture : hint_albedo;
uniform vec4 trans_color : hint_color;

void fragment() {
	float epsilon = 0.8f;

    vec3 day_tex_color = texture(day_texture, UV).rgb;

	// Set regular pixel color from texture
	ALBEDO = day_tex_color;

	if (day_tex_color.r>=trans_color.r - epsilon 
		&& day_tex_color.r<=trans_color.r + epsilon 
	    && day_tex_color.g>=trans_color.g - epsilon
		&& day_tex_color.g<=trans_color.g + epsilon
	    && day_tex_color.b>=trans_color.b - epsilon
		&& day_tex_color.b<=trans_color.b + epsilon) {   
	   discard;
	}
}


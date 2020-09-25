shader_type spatial; 

uniform sampler2D day_texture : hint_albedo;

void fragment() {
    vec3 day_tex_color = texture(day_texture, UV).rgb;
	
	// Set regular pixel color from texture
	ALBEDO = day_tex_color;
}
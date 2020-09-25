shader_type spatial; 

uniform sampler2D day_texture : hint_albedo;
uniform vec4 trans_color : hint_color;

void fragment() {
    vec3 day_tex_color = texture(day_texture, UV).rgb;
	float epsilon = 0.01f;
	
	// Set regular pixel color from texture
	ALBEDO = day_tex_color;
	
	// Discard pixels (transparency) ... TODO, think about epsilon as this seems slow
	// Epsilon fuzzy range is seemingly required because of different floating point precision?
	// i.e. when script-side Color8 is handed to shader r/g/b 0.0-1.0 values
	// similar issue here https://godotengine.org/qa/57568/shader-not-working-at-runtime
    if (day_tex_color.r>=trans_color.r - epsilon 
		&& day_tex_color.r<=trans_color.r + epsilon 
	    && day_tex_color.g>=trans_color.g - epsilon
		&& day_tex_color.g<=trans_color.g + epsilon
	    && day_tex_color.b>=trans_color.b - epsilon
		&& day_tex_color.b<=trans_color.b + epsilon) {   
	   discard = true;
	}
}
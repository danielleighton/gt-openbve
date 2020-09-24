shader_type spatial; 
render_mode cull_disabled;

uniform sampler2D day_texture : hint_albedo;


void fragment() {
    vec3 day_tex_color = texture(day_texture, UV ).rgb;

	// filter out transparent color
	float blue = 255f / 255f;
	
	ALBEDO = day_tex_color;

	// discard pixels (transparency) when 8 bit blue = 255
    if (day_tex_color.r==0.0 && day_tex_color.g==0.0 && day_tex_color.b==1.0) {   
	   discard = true;
	}

	METALLIC = 1.0;
}


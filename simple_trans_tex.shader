shader_type spatial; 
render_mode cull_disabled;
uniform float texDetailRes = 10;
uniform sampler2D tex1Albedo : hint_albedo;
uniform sampler2D tex1Normal: hint_normal;
uniform sampler2D tex2Albedo : hint_albedo;
uniform sampler2D tex2Normal: hint_normal;

void fragment() {
    vec3 a1 = texture(tex1Albedo, UV ).rgb;
    vec3 n1 = texture(tex1Normal, UV ).rgb;
    vec3 a2 = texture(tex2Albedo, UV * texDetailRes).rgb;
    vec3 n2 = texture(tex2Normal, UV * texDetailRes).rgb;
    //vec3 albedoAdd = a1 + a2; 

	// filter out transparent color
	float blue = 255f / 255f;
	
//	if (a1.r==0.0 && a1.g==0.0 && a1.b==1.0) {
 	   //a1.a=0.0;
	//}
	// end filter
	

	vec3 albedoAdd = a1; 
    vec3 resultAlbedo = clamp(albedoAdd,0,1);
	
	ALBEDO =  resultAlbedo;
	
    vec3 addNormals = n1 *0.5 + n2 *0.5 ;        
	

	// discard pixels (transparency) when 8 bit blue = 255
    if (a1.r==0.0 && a1.g==0.0 && a1.b==1.0) {   
	   discard = true;
	}
	//NORMALMAP = clamp(addNormals,0,1);
    //ROUGHNESS = 0.5;
}
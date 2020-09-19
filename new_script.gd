extends MeshInstance
 
const LOG_LEVEL = 1
 
const SIZE = 1.0
const HEIGHT = sqrt(3.0) * SIZE
const HALF_HEIGHT = HEIGHT * 0.5						# Should be 0.86603 for size of 1
const KITE_HEIGHT = (2.0/3.0) * (HALF_HEIGHT)					# 0.57735 for size of 1
const KITE_ORTHOGONAL_HEIGHT = (0.75) * KITE_HEIGHT		# 0.43301, distance from longside to oppo point
const KITE_SIDE_LONG = SIZE / 2.0
const KITE_SIDE_SHORT = (2.0/3.0) * KITE_ORTHOGONAL_HEIGHT	# Should be 0.28868 for size of 1
 
onready var mesh_instance = $MeshInstance
 
var pt1: Vector3 = Vector3(0.0, 0.0, 0.0)
var pt2: Vector3 = Vector3(KITE_ORTHOGONAL_HEIGHT, 0.0, 0.25)
var pt3: Vector3 = Vector3(KITE_SIDE_SHORT, 0.0, 0.5)
var pt4: Vector3 = Vector3(0.0, 0.0, 0.5)
 
var first_tri_first_kite_vertices = PoolVector3Array([pt1, pt2, pt3, pt4])
 
func _ready():
	var array_mesh = ArrayMesh.new()
	var rolling_vertices: PoolVector3Array = first_tri_first_kite_vertices
	for i in 6:
		array_mesh = draw_hex_tri(rolling_vertices, array_mesh)
		for i2 in rolling_vertices.size():
			rolling_vertices[i2] = (point_rotation(rolling_vertices[i2], rolling_vertices[0], 60.0))
 
	apply_color(array_mesh)
 
	# mesh_instance.mesh = array_mesh
	self.mesh = array_mesh
 
func draw_hex_tri(base_verts: PoolVector3Array, local_array_mesh: ArrayMesh):
	var mesh_arrays = []
	mesh_arrays.resize(Mesh.ARRAY_MAX)
 
	var indices = PoolIntArray([0, 1, 2,  0, 2, 3])
	for i in range(3): # Iterate over each kite
		var verts = PoolVector3Array()	# kite-specific vertices

		for i2 in base_verts.size():
			verts.push_back(point_rotation(base_verts[i2], base_verts[2], 120.0 * i))
 
		mesh_arrays[Mesh.ARRAY_VERTEX] = verts
		mesh_arrays[Mesh.ARRAY_INDEX] = indices
 
		local_array_mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, mesh_arrays)
 
	return local_array_mesh
 
func point_rotation(point_to_rotate: Vector3, rotation_origin: Vector3, angle: float) -> Vector3:
	var rads: float = deg2rad(angle)
	var sin_val: float = sin(rads)
	var cos_val: float = cos(rads)
 
	# Translate point to origin
	point_to_rotate.x -= rotation_origin.x
	point_to_rotate.z -= rotation_origin.z
 
	# Rotate point
	var xnew: float = point_to_rotate.x * cos_val - point_to_rotate.z * sin_val
	var znew: float = point_to_rotate.x * sin_val + point_to_rotate.z * cos_val	# Really, though? point_to_rotate.x???
 
	# Translate back
	var return_value: Vector3 = Vector3(xnew + rotation_origin.x, 0.0, znew + rotation_origin.z)
	return return_value
 
func make_materials(n: int) -> Array:	# Generates an array of SpatialMaterials with given length of random colors
	
	var return_array = []
	var rng = RandomNumberGenerator.new()
	rng.randomize()
	for i in range(n):
		var mat = SpatialMaterial.new()
		var color = Color(rng.randf_range(0.0, 1.0), rng.randf_range(0.0, 1.0), rng.randf_range(0.0, 1.0))
		mat.albedo_color = color
	
		return_array.push_back(mat)
	return return_array
 
func apply_color(local_array_mesh: ArrayMesh):
	var surface_count = local_array_mesh.get_surface_count()
	
	var mats = make_materials(surface_count)
	for i in surface_count:
		local_array_mesh.surface_set_material(i, mats[i])

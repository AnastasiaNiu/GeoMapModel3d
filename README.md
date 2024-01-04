# GeoMapModel3d Program Description
## GeoMapModel3d

### Quick Start: 
+ Click the GeoMapModel3d.sln to open this program.
+ Choose GeoMapModelCli to run this code.

#### 【Brief Introduction】Bedrock modeling program based on 2D geological map

#### 【Input parameters for this program】

### Path parameter：
+ stratumLayerPath: Stratigraphic layer paths
+ stratumLinePath: Stratigraphic line path
+ faultLayerPath: Fault line path
+ demPath: Digital Elevation Data Path
+ altitudePointPath: Path of the point of occurrence (attributes of this point of occurrence element need to include: inclination, dip)
+ contourLinePath: Contour path
+ solidtypetxt: Sequential table of stratigraphic properties
+ savePath: Save Path

### Value parameter：
+ Direction: Set profile line direction, set X/Y, data type, string
+ xMax: Maximum value of the x-direction of the profile line range, data type, double
+ xMin: Minimum value of the x-direction of the profile line range, data type, double
+ yMin: Maximum value of profile line range Y direction, data type, double
+ yMax: Minimum value in the Y direction of the profile line range, data type, double
+ stepLength: Profile line interval step, data type, double
+ elevSampleStep: Elevation data sampling step, data type, double
+ zZoomZ: Value magnification factor, data type, double
+ scale: Scaling factor, data type, double
+ virtualDrillResetY: Value to be added to the virtual drill point in the Y direction, data type, double
+ virtualDrillStep: Virtual drill point sampling step, data type, double
+ test data：A homemade inverted folded geologic body as an as an example

#### Specific parameter format examples are detailed in：
..\ GeoMapModelCli\ParameterSettings.json

### Output data：
+ Save Path /origonLine/origonLine_X.shp，Adaptively generated raw profile lines
+ Save Path /404059000000Point.shp, (multiple, corresponding to profile lines), virtual drill points in the graph-cut profile
+ Save Path /404059000000Polygon.shp, (multiple, corresponding to profile lines), stratigraphic profiles in map-cut profiles
+ Save Path /404059000000Polyline.shp, (multiple, corresponding profile lines), stratigraphic profile lines in map-cut sections
+ Save Path /resetxyz.shp，surface virtual drill points with reset xyz values
+ Save Path /allPointsMerge.shp，a collection of virtual drill points from the merging of all stratigraphic profile drill holes, a collection of virtual drill points from the merging of all stratigraphic profile drill holes 
+ Save Path /soildtypeDrills.shp，set of all virtual drill holes connecting stratigraphic attribute tables
+ Save Path /profileDrills.shp，replacement of virtual drill points on the surface with coordinates
+ Save Path /connectDrills.shp，replacement of the set of virtual drill points whose coordinates are connected to the surface points 
+ Save Path /mesh0.shp, (several, every 50 columns merged into one shp), triangular mesh
+ Save Path /rock_1==0.vtk, (several, 'rock' indicates a bedrock fix setting, '_1' corresponds to the stratigraphic value in the stratigraphic attribute table, '==0' corresponds to mesh0.shp)

#### 【Brief description of algorithm】
Bedrock can be categorized into exposed bedrock (exposed at the surface) and subducted bedrock (covered by a thicker loose layer or rock layer) based on its buried characteristics. Considering the available data, only exposed bedrock is modeled here. The algorithmic process is: 1) constructing the map-cut profile, 2) constructing the virtual borehole, 3) constructing the GTP model, and 4) constructing the surface model.
1. Adaptive generation of profile lines. Input the planar geologic map line file and modeling paradigm, and use adaptive generation profile line to obtain the profile line ensemble file.
2. Generate map-cut profile. Input the known planar geologic map files (including line files: contour lines, geologic lines, fault lines, geologic surfaces, production points, bedrock surface data), through the map-cutting profile to review the map-cutting profile line files and surface files.
3. Generate virtual drill holes and obtain virtual drill hole point files.
4. Drill hole processing. Batch processing of virtual drill holes into drill hole point files that meet the modeling requirements.
5. Generate geologic body model. Construct GTP model from virtual drill holes, and finally generate bedrock geologic body model.

DarkSound is an portal-based audio propagation system built for the Unity game engine. This system aims to replecate sound traveling around a virtual environment, simulating effects such as diffraction, obstruction and perception. 

Code Documentation
----------------------------------------------------------------------------------------------
DSAudioListener.cs
----------------------------------------------------------------------------------------------
Variables: 

DSRoom currentRoom - The room that the listener is currently inside of. 

----------------------------------------------------------------------------------------------
Functions: 

DSRoom GetRoomForPosition(Vector3 worldPosition)
Calculates the current room based on the given position

Parameters:
worldPosition - Current position in world space.

Return Value:
The room that this position is contained in. 

----------------------------------------------------------------------------------------------

void AddRoom(DSRoom room)
Adds a room to the list of all rooms in the scene. 

Parameters:
room  - the room to add.

----------------------------------------------------------------------------------------------

void RemoveRoom(DSRoom room)
Removes a room from the list of all rooms in the scene. 

Parameters:
room  - the room to remove.

----------------------------------------------------------------------------------------------

void ClearRoomList()
Removes all rooms from the list of all rooms in the scene. 

----------------------------------------------------------------------------------------------

List<DSRoom> FindPath(DSRoom startingNode, DSRoom endNode, bool nonOptimalPath = false)
Finds a path from one specified room to another. 

Parameters:
startingNode - Room to start path. 
endingNode - Room to end path. 
nonOptimalPath - When true the portal contribution will not be considered. 

Return Value:
A list of rooms that make up the optimal path. 


----------------------------------------------------------------------------------------------




----------------------------------------------------------------------------------------------

List<DSRoom> RetracePath(DSRoom startingNode, DSRoom endNode)
Finds through nodes from the final node to return the final path.  

Parameters:
startingNode - Room to start path. 
endingNode - Room to end path. 

Return Value:
The path from startNode to endNode. 


----------------------------------------------------------------------------------------------
DSAudioSource.cs
----------------------------------------------------------------------------------------------
Variables: 

bool isDynamic - Will this source ever move into different rooms?
bool usePropagations - Does this source calculate propagation effects?
bool playOnAwake - Should this source play on start up?
bool useDirectivity - Should this source simulate direction?   
AnimationCurve falloffCurve - A curve representing the fall off of the audio over distance. 
float maxDistance - Maximum distance that this source is audible from. 
float maxVolume - Volume of source at zero distance. 
LayerMask obstructionLayerMas - the layers that this source can be obstructed by. 

----------------------------------------------------------------------------------------------
Functions: 

void CalculatePropagation(bool initialisationCall = false)
Calculates the propagation of audio from this source. The calculated values are then applied to the source to effect the audio. 

Parameters:
initialisationCall - Is this the first time this has been called? 

----------------------------------------------------------------------------------------------

float  GetObstruction(float portalObstruction)
Returns the maximum obstruction value, whether that be from the linecasts or the portals themselves 

Parameters:
portalObstruction - The obstruction value given from the portal traversal. 

Return Value:
The maximum obstruction value. 

----------------------------------------------------------------------------------------------

float  ObstructionCheck()
Checks direct obstruction between listener and source. This value is calculated from  individual linecast values. 

Return Value:
Returns a value between 0 and 1 to represent the amount of rays obstructed.  

----------------------------------------------------------------------------------------------

int  ObstructionLinecast(Vector3 start, Vector3 end)
Performs a linecast from point start to point end in order to check for obstructions. 

Parameters:
Start - Start position of the linecast in world space. 
End - end position of the linecast in world space. 

Return Value:
Returns a value to represent obstruction. 1 == no obstruction. 0 == no obstruction

----------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------
DSRoom.cs
----------------------------------------------------------------------------------------------
Variables: 

Collider[] boundsColliders - Array of colliders that make up the bounds of this room. 
List<ConnectedRoom> connectedRooms - A list of all rooms connected to this room and the
portals that connect them. 

----------------------------------------------------------------------------------------------
Functions: 


void InitialiseRoom()
Initialised room, setting it up to work with pathfinding etc. 

----------------------------------------------------------------------------------------------

void  AddRoomConnection(DSPortal portal, DSRoom room)
Adds a connection between this room and another through a specified portal

Parameters:
portal - The portal that connects the rooms. 
room - The connected room. 

----------------------------------------------------------------------------------------------

bool PositionIsInRoomBounds(Vector3 worldPosition)
Checks if a specified world position is within the bounds of this room

Parameters:
worldPosition - position to check in world space.

Return Value:
If the specified position is contained within this room. 

----------------------------------------------------------------------------------------------

DSPortal GetPortal(DSRoom room)
Gets the portal that connects this room to another specified room. 

Parameters:
room - the target room. 

Return Value:
The portal that connects this room to the target room. Returns null if none exist. 

----------------------------------------------------------------------------------------------
DSPortal.cs
----------------------------------------------------------------------------------------------

Variables: 

DSRoom firstRoom - The first room that this portal is connected to. 
DSRoom secondRoom - The second room that this portal is connected to. 
float openCloseAmount - Value between 0 and 1 representing how open (0) or closed (1) the portal is. 
float audioObstructionAmount - Value representing how much of the audio can pass through this portal. 
bool open - is this portal fully open?

----------------------------------------------------------------------------------------------
Functions: 

float GetAudioObstructionAmount()
Calculated the amount of obstruction this portal will contribute to audio traveling through it. 

Return Value:
Returns value representing how much audio is absorbed by the portal in its current state. 

----------------------------------------------------------------------------------------------

Vector3  GetClosestPointInBounds(Vector3 worldPosition)
Gets the closest point to another world position within the bounds of this portal. 

Parameters:
worldPosition - the world position to get a point closest too. 

Return Value:
Returns the closest point within the bounds of this portal to the specified world position in world space. 

----------------------------------------------------------------------------------------------





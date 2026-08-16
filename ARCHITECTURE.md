# list driven vs lock driven

## list driven
- for lots of systems, the same set of inputs leads to the same outputs. if you just sync the inputs, then you dont have to sync the outputs
- specifically this is great for flags. usually you do a big OR over all players to determine the actual value.
- e.g quantum visibile = any player visible true
- also works for lists if you union the set of items. e.g. list of illuminating dream lanterns

a bunch of things are purely time based + a random value. if the random is deterministic timesync will handle the rest
if there's physics involved e.g. jellyfish or meteors it might drift too much and will have to resync. will have to test

pros:
- usually less work to write. patching the flags to sync them is way easier having one owner check and perform everything 
- less lantency. dont have to wait for acquire lock, can just respond instantly and other clients will respond later
cons: 
- gotta make sure the all inputs are the same. if you miss one, the resulting action will be different
- this includes when the player is not around. the output might not trigger when the player isnt there, so gotta make sure to do it manually on receive input in that case
examples:
- light sensors. they have a lit flag and list of dream lanterns. can just or/union together those inputs from all players and then each player handles the event firing themselves
- quantum? visibility is a flag you can turn into a list. if any is not visible, event happens. might need an owner to say where it goes


## lock driven
- using owner system (owner system is a lock with force and remove or eventual lock with add and remove)
- owner has write access (sends messages and sets state) and everyone else has read access (receives messages and reads state from them)

pros:
- works for everything
cons:
- latency in acquiring the lock before writing
- way more to write
- gotta send all written state, more to write
examples
- ghosts. there's also so much input state and rewriting things to fit multiple players that having everyone determine output state would be too unreliable
- anglerfish. inputs is player and probe and ship position/noise, easier to just sync the action happening instead of syncing all noise inputs
- items. only one player can hold an item at a time

all list driven systems can be converted to lock driver, but not vice versa

you can split a system into subsystems and use list or locks for them. e.g. quantum might use lists for visiblity flag but locks for performing the state change


## other examples

- geysers: time based + random initial offset. only that input is changed, timesync handles everything
- jellyfish also uses random
- dream candles: being lit is determined entirely by light sensor. no sync needed
- orb doors/switches: not sectored, just controlled by orb
- interact things like gears: not lock or list. just a single event. who cares who does it first
  - might need to lock on someone taking the interaction like slide reel or code gears





# lifecycle

here's my current plans for the game lifecycle:

- host hosts, gets into game. we pause after lateinit is done
- nonhosts join whenever, get into game. same thing, they pause
- host says were ready to go. at this point everyone has joined so theres connections for everyone. we build all qobjects here.
    - we waited for lateinit so everything should be setup. could also just pause as soon as you join and then async like qsb1
    - deterministic path capturing can happen here, or maybe before. rigidbodies are special, so they capture path as soon as they exist with the goofy patch
    - everything else captures path just as we build. i think that should be good enough, but can have path capturing as an explicit step if wanted
- might have separate state for initing managers (which find the qobjects) and initing qobjects
- now all qobjects are done building.
    - this also includes player and probe, which are qobjects
    - literally everything worth thinking about is a qobject. if it exists in the world, it has a qobject
- on start scene load, tear all qobjects down.
- now we reload, then we go back to top (getting into game, pausing after blah, letting people join)

alternatively, instead of a hard lifecycle we just have a bunch of separate flags (like these objects are built for this guy or whatever) and then just do waiting based on those.
that sounds more maleable and easier to handle

UPDATE: i have done this with flags and sync points. way easier than a state machine.

also, updating object builders and objects is the same thing. we only needed to wait for other objects, and we can just use a delay.runwhen for that

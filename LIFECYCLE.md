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

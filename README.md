# The QSB Port and Review Project

QSB has had some longstanding issues that are somewhat obvious in hindsight.
This project aims to swap out the entire foundation of QSB with a more stable one to try and remove some of the more major bugs and architectural grossness.

* No more Mirror. We bypassesd 90% of this, and the bits we used we had to hack around. It has some bad decisions around disconnections that I really wanted to rework, too.
  * This also means no more Mirror weaver. We now use a more standard serialization library that generates ser/deser functions. If addons need their own, they simply include the library.
  * This also means no more weird network transform + world object bs. i hated this and it broke sometimes terribly
* world objects no longer have one shared ID space. this means if one object is mismatching it doesn't effect others (see small explosions section in STANDARDS.md)
  * this also means we can freely create/delete any things without shifting IDs around. we could do this before technically, but never did. this just makes it easier cuz each object type is its own independent thing, which is less brittle
* diagnostic state is communicated between clients more, for hopefully less guesswork. this also means NO MORE LOGS BEHIND DEBUG for now, since things WILL fail randomly and i want FULL LOGS for when they happen.
* no more goofy save sync. this caused some rare but bad problems around overwriting accidentally, and also messed with other mods (nh). either we'll make a new profile or tell players to do it themselves.
* no more joining mid game. this wipes out and entirety category of code (initial state sync) and the bugs associated with not writing that code/writing it wrong.
* no more singleplayer. this was never tested in qsb1 and it turns out there's some problems with it. if you want singleplayer, disable qsb.

## Porting

Every single piece of code is being manually ported over, reviewing as we port. I expect this to take a couple of months, probably more, given I'm really only working on this on some weekends.
I'm hoping we can get rid of or at least document gross stuff while the porting happens, as well as expand the foundation over time, hopefully without as many refactors.

## Bugs

The biggest category of bugs I know of are:
* lifecycle stuff (join/leave, time sync, looping, etc)
* quantum
* owlks
* race conditions (orb lock, item pick up at same time, etc)

hopefully we can address some of those during the port, or at least document them better
# Experimental Proof of Concept for CDDA map generation from OpenStreetMap data

WARNING: This is just a quick proof of concept that places only a few elements (water, roads and not much else) in an empty CDDA world, it's far from being useful.

## Workflow

1. Export OSM XML file from <https://www.openstreetmap.org/export>
2. Create a New World in CDDA. All its content will be removed.
3. In `CddaOsmMaps\CddaOsmMaps\Program.cs` [I know, this should be configurable with command line args]:
    - Set `CDDA_FOLDER` pointing to `cdda` folder
    - Set `OSM_XML_FILEPATH` with the path to the OSM XML file
4. Run

Discussion forum posts:

- Reddit: <https://www.reddit.com/r/cataclysmdda/comments/mvij7b>
- Discourse: <https://discourse.cataclysmdda.org/t/26238>

## Attribution

This project contains a modified version of [OsmSharp](https://github.com/OsmSharp/core) ([MIT License](https://github.com/OsmSharp/core/blob/develop/LICENSE.md) - Copyright (c) 2017 Ben Abelshausen) to workaround [the problem with Complete Relations when some nodes are missing](https://github.com/OsmSharp/core/issues/63).

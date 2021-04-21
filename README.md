# Experimental Proof of Concept for CDDA map generation from OpenStreetMap data

WARNING: This is just a quick proof of concept that places only roads in an empty CDDA world, it's far from being useful.

## Workflow

1. Export OSM XML file from <https://www.openstreetmap.org/export>
2. Set `OSM_FILEPATH` with the path to the OSM XML file on `parse_osm.py` and run.
3. Create a New World in CDDA. All its content will be removed.
4. Set `CDDA_FOLDER` and `SAVEGAME` in `generate_cdda_map.py` and run.

Reddit post with a bit more (not much) info:
<https://www.reddit.com/r/cataclysmdda/comments/mvij7b>

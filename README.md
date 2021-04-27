# Experimental Proof of Concept for CDDA map generation from OpenStreetMap data

WARNING: This is just a quick proof of concept that places only a few elements (water, roads and not much else) in an empty CDDA world, it's far from being useful.

## Workflow

1. Export OSM file from <https://www.openstreetmap.org/export>, <https://export.hotosm.org> or any other source
2. Create a New World in CDDA. All its content will be removed.
3. Run command line:

```text
CddaOsmMaps
  Generates Cataclysm CDDA maps from OpenStreetMap data (OSM XML and PBF)

  Example:
    CddaOsmMaps.exe ^
      -cdda "C:\CDDA Game Launcher\cdda" ^
      -save "Real World" ^
      -osm "Boston.osm.pbf" ^
      -bounds 42.35 -71.06 42.37 -71.02 ^
      -spawn 42.36 -71.04 ^
      -ppm 1.2 ^
      -img bostom-map.png ^
      -v

Usage:
  CddaOsmMaps [options]

Options:
  -cdda, --cdda-path <cdda-path> (REQUIRED)       Path to Cataclysm DDA folder
  -save, --save-game <save-game>                  Name of the CDDA world save in which the map will be generated
                                                  WARNING: All content will be deleted
                                                  If not provided only the image will be generated
  -osm, --osm-filepath <osm-filepath> (REQUIRED)  Path to the OSM file (PBF or XML format)
  -bounds, --gis-bounds <gis-bounds>              Bounding box for the map, 4 values in this order:
                                                  [MinLatitude], [MinLongitude], [MaxLatitude], [MaxLongitude]
                                                  REQUIRED only if OSM file does not contain <bounds> element
  -spawn, --spawn-point <spawn-point>             Latitude and longitude of player position. Default value: Bounding box center
  -ppm, --pixels-per-meter <pixels-per-meter>     Map resolution. One pixel corresponds to one CDDA tile [default: 1,2]
  -img, --image-filepath <image-filepath>         Intermediate image (PNG) will be saved to this file
  -v, --verbose                                   Logs all warning messages
  --version                                       Show version information
  -?, -h, --help                                  Show help and usage information
```

Discussion forum posts:

- Reddit: <https://www.reddit.com/r/cataclysmdda/comments/mvij7b>
- Discourse: <https://discourse.cataclysmdda.org/t/26238>

## Attribution

This project contains a modified version of [OsmSharp](https://github.com/OsmSharp/core) ([MIT License](https://github.com/OsmSharp/core/blob/develop/LICENSE.md) - Copyright (c) 2017 Ben Abelshausen) to workaround [the problem with Complete Relations when some nodes are missing](https://github.com/OsmSharp/core/issues/63).

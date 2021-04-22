import math
import xml.etree.ElementTree as ET
from PIL import Image, ImageDraw


############################################################ PARAMS
OSM_FILEPATH = '.doc-osm - saint-johnsbury.osm'
PIXELS_PER_METER = float(1)
############################################################


OUTPUT_FILE = 'roads.png'
PIL_IMG_FORMAT = 'RGB'
PIL_IMG_BGR = 'white'

# Length in meters of 1° of latitude = always 111.32 km
METERS_PER_LAT_DEG = 111319
METERS_PER_LON_DEG_FN = lambda lat: 40075000 * math.cos(math.radians(lat)) / 360

DEFAULT_ROAD_TYPE_WIDTH = int( 8 * PIXELS_PER_METER)
ROAD_TYPE_WIDTHS = {
    # https://wiki.openstreetmap.org/wiki/Key:highway
    'motorway':      int(12 * PIXELS_PER_METER),
    'motorway_link': int(10 * PIXELS_PER_METER),
    'trunk':         int(10 * PIXELS_PER_METER),
    'trunk_link':    int( 8 * PIXELS_PER_METER),
    'primary':       int( 9 * PIXELS_PER_METER),
    'secondary':     int( 8 * PIXELS_PER_METER),
    'tertiary':      int( 8 * PIXELS_PER_METER),
    'tertiary_link': int( 6 * PIXELS_PER_METER),
    'unclassified':  int( 8 * PIXELS_PER_METER),
    'residential':   int( 8 * PIXELS_PER_METER),
    'living_street': int( 8 * PIXELS_PER_METER),
    'service':       int( 6 * PIXELS_PER_METER),
    'construction':  int( 6 * PIXELS_PER_METER),
    'track':         int( 5 * PIXELS_PER_METER),
    'pedestrian':    int( 4 * PIXELS_PER_METER),
    'cycleway':      int( 4 * PIXELS_PER_METER),
    'path':          int( 4 * PIXELS_PER_METER),
    'footway':       int( 4 * PIXELS_PER_METER),
    'steps':         int( 3 * PIXELS_PER_METER),
}

BOUNDS_KEY = 'bounds'
MIN_LAT_KEY = 'minlat'
MIN_LON_KEY = 'minlon'
MAX_LAT_KEY = 'maxlat'
MAX_LON_KEY = 'maxlon'
ID_KEY = 'id'
LAT_KEY = 'lat'
LON_KEY = 'lon'
NODE_KEY = 'node'
WAY_KEY = 'way'
WAY_NODE_KEY = 'nd'
WAY_NODE_REF_KEY = 'ref'
TAG_KEY = 'tag'
TAG_KEY_ATTR_KEY = 'k'
TAG_VALUE_ATTR_KEY = 'v'
TAG_HIGHWAY_ATTR_VALUE = 'highway'
TAG_BUILDING_ATTR_KEY = 'building'
YES = 'yes'


def run():
    draw_map() \
        .transpose(Image.ROTATE_90) \
        .save(OUTPUT_FILE)


def draw_map():
    root = ET.parse(OSM_FILEPATH).getroot()
    (bounds, bound_sizes, scales) = get_map_bounds_and_scales(root)

    img_size = scale_coord(scales,  bound_sizes)
    img = Image.new(PIL_IMG_FORMAT, img_size, color = PIL_IMG_BGR)
    draw = ImageDraw.Draw(img)

    draw_map_elements(root, bounds, scales, draw)
    root.clear()

    return img


def get_map_bounds_and_scales(root):
    # <bounds minlat='40.4352000' minlon='-3.8306000' maxlat='40.4559000' maxlon='-3.7920000'/>
    bounds = root.find(BOUNDS_KEY).attrib
    for key in bounds:
        bounds[key] = float(bounds[key])

    # Length in meters of 1° of longitude = 40075 km * cos( latitude ) / 360
    avg_lat = (bounds[MAX_LAT_KEY] + bounds[MIN_LAT_KEY]) / 2
    meters_per_lon_deg = METERS_PER_LON_DEG_FN(avg_lat)

    scales = (
        METERS_PER_LAT_DEG * PIXELS_PER_METER,
        meters_per_lon_deg * PIXELS_PER_METER
    )
    bound_sizes = (
        bounds[MAX_LAT_KEY] - bounds[MIN_LAT_KEY],
        bounds[MAX_LON_KEY] - bounds[MIN_LON_KEY]
    )

    return (bounds, bound_sizes, scales)


def draw_map_elements(root, bounds, scales, draw):
    nodes = dict(map(map_node, root.findall(NODE_KEY)))
    ways = root.findall(WAY_KEY)

    draw_buildings(ways, nodes, bounds, scales, draw)
    draw_roads(ways, nodes, bounds, scales, draw)


def map_node(node):
    # <node id='30873350' visible='true' version='10' changeset='45631211'
    #       timestamp='2017-01-29T20:27:50Z' user='mor' uid='220932'
    #       lat='40.4562552' lon='-3.8037791'/>
    return (
        node.attrib[ID_KEY],
        (
            float(node.attrib[LAT_KEY]),
            float(node.attrib[LON_KEY])
        )
    )


def draw_roads(ways, nodes, bounds, scales, draw):
    for way in ways:
        draw_road(nodes, bounds, scales, draw, way)


def draw_road(nodes, bounds, scales, draw, way):
    road_type = get_road_type(way)
    if road_type is None:
        return

    node_xys = list(map(
        lambda node: get_node_xy(bounds, scales, nodes, node),
        way.findall(WAY_NODE_KEY)
    ))

    if not road_type in ROAD_TYPE_WIDTHS:
        print(f'UNKOWN ROAD TYPE {road_type}')

    draw.line(
        node_xys,
        fill='black',
        width=ROAD_TYPE_WIDTHS[road_type]
            if road_type in ROAD_TYPE_WIDTHS
            else DEFAULT_ROAD_TYPE_WIDTH,
        joint='curve'
    )


def get_road_type(way):
    # <way ...>
    #  <tag k='highway' v='motorway'/>
    # </way>
    for tag in way.findall(TAG_KEY):
        if (TAG_KEY_ATTR_KEY in tag.attrib
            and TAG_VALUE_ATTR_KEY in tag.attrib
            and tag.attrib[TAG_KEY_ATTR_KEY] == TAG_HIGHWAY_ATTR_VALUE
        ):
            return tag.attrib[TAG_VALUE_ATTR_KEY]

    return None


def draw_buildings(ways, nodes, bounds, scales, draw):
    for way in ways:
        draw_building(nodes, bounds, scales, draw, way)


def draw_building(nodes, bounds, scales, draw, way):
    if not is_building(way):
        return

    node_xys = list(map(
        lambda node: get_node_xy(bounds, scales, nodes, node),
        way.findall(WAY_NODE_KEY)
    ))

    draw.polygon(
        node_xys,
        fill='#eeeeff22',
        outline='blue'
    )


def is_building(way):
    # <way ...>
    #   <tag k='building' v='yes'/>
    # </way>
    for tag in way.findall(TAG_KEY):
        if (TAG_KEY_ATTR_KEY in tag.attrib
            and TAG_VALUE_ATTR_KEY in tag.attrib
            and tag.attrib[TAG_KEY_ATTR_KEY] == TAG_BUILDING_ATTR_KEY
            and tag.attrib[TAG_VALUE_ATTR_KEY] == YES
        ):
            return True

    return False


def get_node_xy(bounds, scales, nodes, node_elem):
    node_id = node_elem.attrib[WAY_NODE_REF_KEY]
    (node_lat, node_lon) = nodes[node_id]
    return scale_coord(
        scales,
        (
            node_lat - bounds[MIN_LAT_KEY],
            node_lon - bounds[MIN_LON_KEY]
        )
    )


def scale_coord(scales, coords):
    return (
        int(coords[0] * scales[0]),
        int(coords[1] * scales[1])
    )


if __name__ == '__main__':
    run()

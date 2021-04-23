import json
import math
import os
import shutil
from collections import namedtuple
from PIL import Image


############################################################ PARAMS
CDDA_FOLDER = r'C:\workspace\Games\CDDA Game Launcher\cdda'
SAVEGAME = 'Empty World'
############################################################


Coords = namedtuple('Coords', ['segment', 'submap_4x_file', 'submap_idx', 'submap_relpos'])

ROADS_FILE = 'roads.png'
CDDA_SAVE_FOLDER = 'save'
CDDA_SAVE_SEGMENTS_FOLDER = 'maps'
MAIN_SAVE_FILE_EXT = '.sav'
SAVE_VERSION = 33
SAVE_VERSION_HEADER = f'# version {SAVE_VERSION}\n'

SEEN_0_0_FILE_EXT = '.seen.0.0'
EMPTY_SEEN_DATA_COUNT = 21
EMPTY_SEEN_0_0 = {
    'visible': [[[False, 32400]]] * EMPTY_SEEN_DATA_COUNT,
    'explored': [[[False, 32400]]] * EMPTY_SEEN_DATA_COUNT,
    'notes': [[]] * EMPTY_SEEN_DATA_COUNT,
    'extras': [[]] * EMPTY_SEEN_DATA_COUNT
}

MAP_MEMORY_FILE_EXT = '.mm'
EMPTY_MAP_MEMORY = [[],[]]

O_0_0_FILE = 'o.0.0'
EMPTY_O_0_0 = {
    'layers': ([[['empty_rock',32400]]]*10)
        + [[['field',32400]]]
        + ([[['open_air',32400]]]*10),
    'region_id': 'default',
    'monster_groups': [],
    'cities': [],
    'connections_out': {},
    'radios': [],
    'monster_map': [],
    'tracked_vehicles': [],
    'scent_traces': [],
    'npcs': [],
    'camps': [],
    'overmap_special_placements': []
}

ACTIVE_MONSTERS_KEY = 'active_monsters'
STAIR_MONSTERS_KEY = 'stair_monsters'
LEVX_KEY = 'levx'
LEVY_KEY = 'levy'
PLAYER_KEY = 'player'
PLAYER_POSX_KEY = 'posx'
PLAYER_POSY_KEY = 'posy'
SUBMAP_TERRAIN_KEY = 'terrain'
SUBMAP_COORD_KEY = 'coordinates'

SUBMAP_SIZE = 12
SUBMAP_x4_SIZE = SUBMAP_SIZE * 2
SUBMAP_x4_PER_SEGMENT = 32
SEGMENT_SIZE = SUBMAP_x4_PER_SEGMENT * SUBMAP_x4_SIZE

TILE_TYPE_ROAD = 't_pavement'
TILE_TYPE_DEFAULT = 't_grass'


def run():
    save_path = os.path.join(CDDA_FOLDER, CDDA_SAVE_FOLDER, SAVEGAME)
    save_id = get_save_id(save_path)
    # print('save_id', save_id)

    main_save_file_data = \
        clean_save_and_get_mainsave_data(save_path, save_id)

    player_abspos = get_player_abspos(main_save_file_data)
    player_coords = get_coords(player_abspos)
    # print('player_abspos', player_abspos)
    # print('player_coords', player_coords)

    roads_img = Image.open(ROADS_FILE)

    map_top_left_abspos = (
        int(player_abspos[0] - ( roads_img.width / 2 )),
        int(player_abspos[1] - ( roads_img.height / 2 )),
    )
    map_bot_rght_abspos = (
        map_top_left_abspos[0] + roads_img.width,
        map_top_left_abspos[1] + roads_img.height,
    )
    map_top_left_coords = get_coords(map_top_left_abspos)
    map_bot_rght_coords = get_coords(map_bot_rght_abspos)

    # print('map_top_left_abspos', map_top_left_abspos)
    # print('map_bot_rght_abspos', map_bot_rght_abspos)
    # print('map_top_left_coords', map_top_left_coords)
    # print('map_bot_rght_coords', map_bot_rght_coords)

    segment_x_from = map_top_left_coords.segment[0]
    segment_x_to = map_bot_rght_coords.segment[0]

    segment_y_from = map_top_left_coords.segment[1]
    segment_y_to = map_bot_rght_coords.segment[1]

    for segment_x in range(segment_x_from, segment_x_to + 1):
        for segment_y in range(segment_y_from, segment_y_to + 1):
            generate_segment(
                roads_img,
                map_top_left_abspos,
                map_bot_rght_coords,
                save_path,
                segment_x_from,
                segment_x_to,
                segment_y_from,
                segment_y_to,
                segment_x,
                segment_y
            )


def get_save_id(save_path):
    save_files = os.listdir(save_path)
    main_save_filename = [ file
        for file in save_files
        if file.endswith(MAIN_SAVE_FILE_EXT)
    ][0]

    return main_save_filename[:-len(MAIN_SAVE_FILE_EXT)]


def clean_save_and_get_mainsave_data(save_path, save_id):
    clean_segment_map_files(save_path)

    write_json(
        save_path,
        filename=f'{save_id}{SEEN_0_0_FILE_EXT}',
        data=EMPTY_SEEN_0_0,
        header=SAVE_VERSION_HEADER
    )

    write_json(
        save_path,
        filename=O_0_0_FILE,
        data=EMPTY_O_0_0,
        header=SAVE_VERSION_HEADER
    )

    write_json(
        save_path,
        filename=f'{save_id}{MAP_MEMORY_FILE_EXT}',
        data=EMPTY_MAP_MEMORY
    )

    main_save_filename = f'{save_id}{MAIN_SAVE_FILE_EXT}'
    main_save_file_data = read_json(
        save_path,
        main_save_filename,
        header_size=1
    )
    main_save_file_data[ACTIVE_MONSTERS_KEY] = []
    main_save_file_data[STAIR_MONSTERS_KEY] = []

    write_json(
        save_path,
        filename=main_save_filename,
        data=main_save_file_data,
        header=SAVE_VERSION_HEADER
    )

    return main_save_file_data


def clean_segment_map_files(save_path):
    segment_map_filepaths = os.path.join(save_path, CDDA_SAVE_SEGMENTS_FOLDER)

    if os.path.exists(segment_map_filepaths):
        shutil.rmtree(segment_map_filepaths)

    os.mkdir(segment_map_filepaths)


def get_player_abspos(main_save_file_data):
    # TEST DATA (save_id.sav JSON file)
    # levx,y            =  251,  43
    # player posx,y     =   66,  67

    # ABSOLUTE POSITION player_abspos
    # (lev x 12) + ppos = 3078, 583

    levx = main_save_file_data[LEVX_KEY]
    levy = main_save_file_data[LEVY_KEY]
    player_data = main_save_file_data[PLAYER_KEY]
    player_posx = player_data[PLAYER_POSX_KEY]
    player_posy = player_data[PLAYER_POSY_KEY]

    return (
        (levx * SUBMAP_SIZE) + player_posx,
        (levy * SUBMAP_SIZE) + player_posy,
    )


def generate_segment(
    roads_img,
    map_top_left_abspos,
    map_bot_rght_coords,
    save_path,
    segment_x_from,
    segment_x_to,
    segment_y_from,
    segment_y_to,
    segment_x,
    segment_y
):
    segment_path = os.path.join(
        save_path,
        CDDA_SAVE_SEGMENTS_FOLDER,
        f'{segment_x}.{segment_y}.0'
    )
    os.mkdir(segment_path)

    submap_4x_file_x_from = (
        segment_x * SUBMAP_x4_PER_SEGMENT
        if segment_x >= segment_x_from
        else map_top_left_coords.submap_4x_file[0]
    )
    submap_4x_file_x_to = (
        (segment_x + 1) * SUBMAP_x4_PER_SEGMENT
        if segment_x < segment_x_to
        else map_bot_rght_coords.submap_4x_file[0]
    )

    submap_4x_file_y_from = (
        segment_y * SUBMAP_x4_PER_SEGMENT
        if segment_y >= segment_y_from
        else map_top_left_coords.submap_4x_file[1]
    )
    submap_4x_file_y_to = (
        (segment_y + 1) * SUBMAP_x4_PER_SEGMENT
        if segment_y < segment_y_to
        else map_bot_rght_coords.submap_4x_file[1]
    )

    for submap_4x_file_x in range(submap_4x_file_x_from, submap_4x_file_x_to):
        for submap_4x_file_y in range(submap_4x_file_y_from, submap_4x_file_y_to):
            generate_submap_4x_file(
                roads_img,
                map_top_left_abspos,
                segment_path,
                submap_4x_file_x,
                submap_4x_file_y
            )


def generate_submap_4x_file(
    roads_img,
    map_top_left_abspos,
    segment_path,
    submap_4x_file_x,
    submap_4x_file_y
):
    submap_4x_data = []

    for submap_idx_x in range(2):
        for submap_idx_y in range(2):
            submap = get_sumap(
                roads_img,
                map_top_left_abspos,
                submap_4x_file_x,
                submap_4x_file_y,
                submap_idx_x,
                submap_idx_y
            )
            submap_4x_data.append(submap)

    submap_4x_filename = f'{submap_4x_file_x}.{submap_4x_file_y}.0.map'
    # print(f'{segment_path}{submap_4x_filename}')

    write_json(segment_path, submap_4x_filename, submap_4x_data, header=None)


def get_sumap(
    roads_img,
    map_top_left_abspos,
    submap_4x_file_x,
    submap_4x_file_y,
    submap_idx_x,
    submap_idx_y
):
    submap = create_submap()
    # Examples:
    #   99.557.map['coordinates'] = [ [ 198, 1114 ], [ 198, 1115 ], [ 199, 1114 ], [ 199, 1115 ] ]
    #   36.546.map['coordinates'] = [ [ 72, 1092 ], [ 72, 1093 ], [ 73, 1092 ], [ 73, 1093 ] ]
    submap[SUBMAP_COORD_KEY] = [
        (submap_4x_file_x * 2) + submap_idx_x,
        (submap_4x_file_y * 2) + submap_idx_y,
        0
    ]

    terrain = get_sumap_terrain(
        roads_img,
        map_top_left_abspos,
        submap_4x_file_x,
        submap_4x_file_y,
        submap_idx_x,
        submap_idx_y
    )

    submap[SUBMAP_TERRAIN_KEY] = simplify_terrain(terrain)

    return submap


def get_sumap_terrain(
    roads_img,
    map_top_left_abspos,
    submap_4x_file_x,
    submap_4x_file_y,
    submap_idx_x,
    submap_idx_y
):
    terrain = []
    for submap_tile_x in range(SUBMAP_SIZE):
        for submap_tile_y in range(SUBMAP_SIZE):
            tile_abspos = get_abspos(
                submap_4x_file=(submap_4x_file_x, submap_4x_file_y),
                submap_idx=(submap_idx_x, submap_idx_y),
                submap_relpos=(submap_tile_y, submap_tile_x)  # reversed X and Y !!!
            )

            pixel_pos = (
                tile_abspos[0] - map_top_left_abspos[0],
                tile_abspos[1] - map_top_left_abspos[1]
            )

            tile_type = (
                TILE_TYPE_ROAD
                if (    0 <= pixel_pos[0] < roads_img.width
                    and 0 <= pixel_pos[1] < roads_img.height
                    and roads_img.getpixel(pixel_pos)[0] == 0
                ) else TILE_TYPE_DEFAULT
            )

            terrain.append(tile_type)

    return terrain


def get_coords(abspos):
    # SEGMENT                                         !!!
    # floor(abspos / 768) =    4,   0
    segment = (
        math.floor(abspos[0] / SEGMENT_SIZE),
        math.floor(abspos[1] / SEGMENT_SIZE)
    )

    # IN SEGMENT RELATIVE POSITION relpos_in_segment
    # abspos % 768        =    6, 583
    relpos_in_segment = (
        abspos[0] % SEGMENT_SIZE,
        abspos[1] % SEGMENT_SIZE
    )

    # IN SEGMENT RELATIVE 4x SUBMAP rel_submap_4x_file
    # floor(relpos_in_segment / 24) =   0, 24
    rel_submap_4x_file = (
        math.floor(relpos_in_segment[0] / SUBMAP_x4_SIZE),
        math.floor(relpos_in_segment[1] / SUBMAP_x4_SIZE)
    )

    # ABSOLUTE 4x SUBMAP FILE                         !!!
    # rel_submap_4x_file + segment * 32  = 128, 24
    submap_4x_file = (
        rel_submap_4x_file[0] + (segment[0] * SUBMAP_x4_PER_SEGMENT),
        rel_submap_4x_file[1] + (segment[1] * SUBMAP_x4_PER_SEGMENT)
    )

    # SUBMAP INDEX IN 4xSUBMAP FILE                   !!!
    # floor(relpos_in_segment / 12) - (2 x rel_submap_4x_file)
    #                           = (0, 48) - 2x(0, 24)
    #                           = 0, 0
    submap_idx_in_4x_file = (
        math.floor(relpos_in_segment[0] / SUBMAP_SIZE) - (2 * rel_submap_4x_file[0]),
        math.floor(relpos_in_segment[1] / SUBMAP_SIZE) - (2 * rel_submap_4x_file[1])
    )

    # IN SUBMAP RELATIVE POSITION                     !!!
    # abspos % 12               = 6, 7
    submap_relpos = (
        abspos[0] % SUBMAP_SIZE,
        abspos[1] % SUBMAP_SIZE
    )

    return Coords(
        segment,
        submap_4x_file,
        submap_idx_in_4x_file,
        submap_relpos
    )


def get_abspos(submap_4x_file, submap_idx, submap_relpos):
    return (
        (
            (submap_4x_file[0] * SUBMAP_x4_SIZE)
            + (submap_idx[0] * SUBMAP_SIZE)
            + submap_relpos[0]
        ),
        (
            (submap_4x_file[1] * SUBMAP_x4_SIZE)
            + (submap_idx[1] * SUBMAP_SIZE)
            + submap_relpos[1]
        )
    )


def create_submap():
    return {
        'version': SAVE_VERSION,
        SUBMAP_COORD_KEY: [ 0,0,0 ],
        'turn_last_touched': 1,
        'temperature': 0,
        SUBMAP_TERRAIN_KEY: [],
        'radiation': [ 0, 144 ],
        'furniture': [],
        'items': [],
        'traps': [],
        'fields': [],
        'cosmetics': [],
        'spawns': [],
        'vehicles': [],
        'partial_constructions': []
    }


def simplify_terrain(terrain):
    simplified = []
    tmp_tile_info = [None, 0]

    for tile in terrain:
        if tile == tmp_tile_info[0]:
            tmp_tile_info[1] += 1
        else:
            if tmp_tile_info[1] == 1:
                simplified.append(tmp_tile_info[0])
            elif tmp_tile_info[1] > 1:
                simplified.append(list(tmp_tile_info))

            tmp_tile_info = [tile, 1]

    if tmp_tile_info[1] == 1:
        simplified.append(tmp_tile_info[0])
    elif tmp_tile_info[1] > 1:
        simplified.append(list(tmp_tile_info))

    return simplified


def read_json(save_path, filename, header_size=0):
    json_filepath = os.path.join(save_path, filename)
    with open(json_filepath, 'r', encoding='utf8') as json_file:
        json_text = json_file.readlines()[header_size:]
        return json.loads(''.join(json_text))


def write_json(save_path, filename, data, header=None):
    json_filepath = os.path.join(save_path, filename)
    with open(json_filepath, 'w', encoding='utf8') as json_file:
        if header is not None:
            json_file.write(header)

        json.dump(data, json_file)


if __name__ == '__main__':
    run()

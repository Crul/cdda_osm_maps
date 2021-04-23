import parse_osm
from memory_profiler import profile

def run_profile():
    profile(precision=2)(parse_osm.draw_map)()

if __name__ == '__main__':
    run_profile()

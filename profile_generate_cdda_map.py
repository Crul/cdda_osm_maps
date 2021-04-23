import generate_cdda_map
from memory_profiler import profile

def run_profile():
    profile(precision=2)(generate_cdda_map.run)()

if __name__ == '__main__':
    run_profile()

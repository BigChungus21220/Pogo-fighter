# Pogo-fighter

inputs:
- angle to vertical & velocity
- angle about vertical & velocity
- poker horizontal angle & velocity
- poker vertical angle & velocity
- overall position & velocity

- opponent's above parameters

outputs:
- accelerations for all axies
- jump activation

optimization:
- reinforcement learning for initial training
  - loss is angle to vertical & horizontal angular velocities
  - could also add log loss distance to enemy
- genetic algorithm for training against opponents

- added beans


venv setup (windows)
- install python 3.8.10 with `winget install Python.Python.3.8 --scope machine` in an administrator terminal
- navigate to Pogo-figter-unity/Pogo-fighter-unity
- run `py -3.8 -m venv venv`
- run `./venv/Scripts/activate`
- run `pip install mlagents`
- run `pip install torch`
- run `pip install protobuf~=3.20` to fix some dumb package shit
- run `pip install onnx`
- run `mlagents-learn --run-id=<a string>` to start a session

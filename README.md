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
- create venv `py -3.10 -m venv venv`
- enter venv `./venv/Scripts/activate`
- install packages `pip install -r requirements.txt`

run a training sesh: 
`mlagents-learn <config file path> --run-id=<output folder name> --force --results-dir="Assets/trainedmodels" `

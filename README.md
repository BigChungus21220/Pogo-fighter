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

# how to

## install tools
unity
  install unity
  create a new project
python 3.10
  install python 3.10
    windows: `winget install Python.Python.3.10 --scope machine` in an administrator terminal

## setup venv and install required libraries
(idk what goes here)

## create scene and rig model
create a ground plane + add colision
rig model with armature bodies
  tune parameters
  damping should be 0 for the springs
setup model parameters

## create model controller
setup
  get relevant gameobjects in scene
  disable colision between intersecting parts of armature

applying actions to model

calculating reward
  detect colisions with the ground
  apply penalties
  use center of mass in calculations for better training

## configure trainer
see unity docs for rough numbers

## train model
--resume flag can be used to continue training
--initialize-from={run_id or checkpoint file path} option can be used to train from an existing model without resuming with the same training parameters

## iterate





venv setup (windows)
- 
- navigate to Pogo-figter-unity/Pogo-fighter-unity
- create venv `py -3.10 -m venv venv`
- enter venv `./venv/Scripts/activate`
- install packages `pip install -r requirements.txt`

run a training sesh: 
`mlagents-learn <config file path> --run-id=<output folder name> --force --results-dir="Assets/trainedmodels" `

setup config to intialize from previous checkpoint:
```yaml
behaviors:
  Bouncing:
    init_path: "./Assets/trainedmodels/ppo/<folder name>/<checkpoint name>.pt" # eg: `./Assets/trainedmodels/ppo/Bouncing-base/Bouncing-3315108.pt` to init from the bouncing model
```
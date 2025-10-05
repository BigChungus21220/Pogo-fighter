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

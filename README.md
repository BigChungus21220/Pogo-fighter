# Pogo-fighter

## Running the trained model (Windows)
Run `/Pogo-fighter-unity/Build/Pogo-fighter-unity.exe`

## Compiling the Executable from Source (Windows)
- Install Unity Hub from `unity.com/download`
- Ensure python 3.10 is installed
- Import the project in Unity Hub
- Open the project (this will install required packages)
- Click `File > Build and Run` in the Unity Editor to build and run the app

### Virtual Environment Setup (required for training):
- Navigate to Pogo-figter-unity/Pogo-fighter-unity in a terminal
- Create venv `py -3.10 -m venv venv`
- Enter venv `./venv/Scripts/activate`
- Install packages `pip install -r requirements.txt`

### Running a Training Session: 
Enter the venv, then run `mlagents-learn <config file path> --run-id=<output folder name> --force --results-dir="Assets/trainedmodels" `\
The `--resume` flag can be used to continue training from the last session\
The `--initialize-from={run_id or checkpoint file path}` option can be used to train from an existing model without resuming with the same training parameters

Setup config to intialize from previous checkpoint:
```yaml
behaviors:
  Bouncing:
    init_path: "./Assets/trainedmodels/ppo/<folder name>/<checkpoint name>.pt" # eg: `./Assets/trainedmodels/ppo/Bouncing-base/Bouncing-3315108.pt` to init from the original bouncing model
```
py -m venv venv
venv\Scripts\activate
py -m pip install --upgrade pip
py -m pip install torch~=2.2.1 --index-url https://download.pytorch.org/whl/cu121
python -m pip install mlagents
$login = Get-ECRLoginCommand -Region us-east-2
$login.Password | docker login --username AWS --password-stdin $login.Endpoint

docker-compose build
docker-compose push
eb deploy

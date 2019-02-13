$login = Get-ECRLoginCommand -Region us-east-2
if ($login.ExpiresAt -le (get-date)) {
  $login.Password | docker login --username AWS --password-stdin $login.Endpoint
}

docker-compose build
docker-compose push
eb deploy

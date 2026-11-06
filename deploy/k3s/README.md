# SAT-Rural en una VM con K3s

Este despliegue usa una VM x86/64 con Ubuntu 24.04 LTS (también sirve la imagen Minimal), K3s de un nodo, Traefik y cert-manager. Solo el frontend recibe tráfico público; el Gateway y las bases de datos quedan dentro del clúster.

## 1. Antes de trasladar datos

En el proyecto local, deja de usar la aplicación durante el respaldo. No ejecutes `docker compose down -v`. Desde PowerShell:

```powershell
.\deploy\k3s\make-source-bundle.ps1
.\deploy\k3s\backup-local.ps1
```

Se crean `sat-rural-source.tar.gz` y `sat-rural-data.tar.gz` en la raíz. El código no contiene `.env`; sube ese archivo por separado. Los respaldos incluyen datos personales y deben eliminarse de la VM cuando termine la restauración.

## 2. Preparar la VM

En Compute Engine crea una VM `e2-standard-4` (4 vCPU, 16 GB) en `us-central1-c`, con Ubuntu 24.04 LTS, 100 GB de disco equilibrado, IPv4 externa y tráfico HTTP/HTTPS permitido. Abre **SSH** desde la lista de instancias. En esa terminal:

```bash
sudo apt update
sudo apt install -y curl ca-certificates docker.io tar gzip
sudo systemctl enable --now docker
curl -sfL https://get.k3s.io | INSTALL_K3S_CHANNEL=v1.36 sh -
sudo systemctl is-active k3s
sudo k3s kubectl get nodes -o wide
```

Desde la ventana SSH del navegador, usa el botón **Subir archivo** para subir los dos paquetes y `.env` a tu directorio personal. Después:

```bash
mkdir -p ~/sat-rural
tar -xzf ~/sat-rural-source.tar.gz -C ~/sat-rural
mv ~/.env ~/sat-rural/.env
chmod 600 ~/sat-rural/.env
mkdir -p ~/sat-rural/deploy-backups
tar -xzf ~/sat-rural-data.tar.gz -C ~/sat-rural/deploy-backups
cd ~/sat-rural
```

## 3. Cargar imágenes y restaurar bases

El paquete no sube imágenes ni dependencias compiladas. Se construyen en la VM y se importan en K3s sin publicarlas en un registro externo:

```bash
bash deploy/k3s/build-images.sh
```

Define el subdominio gratuito que apunta a la IPv4 de la VM y tu correo para Let's Encrypt:

```bash
DOMAIN='tu-subdominio.duckdns.org'
EMAIL='tu-correo@example.com'
sudo k3s kubectl create namespace sat-rural
sudo k3s kubectl -n sat-rural create secret generic sat-rural-env --from-env-file=.env
sudo k3s kubectl -n sat-rural create configmap sat-rural-config --from-literal="PUBLIC_URL=https://$DOMAIN"
sudo k3s kubectl apply -f deploy/k3s/databases.yaml
sudo k3s kubectl -n sat-rural get pods
```

Cuando las cinco bases estén `Running` y listas, restaura los respaldos **una sola vez**, antes de iniciar los microservicios:

```bash
bash deploy/k3s/restore-data.sh
sudo k3s kubectl apply -f deploy/k3s/services.yaml
sudo k3s kubectl -n sat-rural get pods
bash deploy/k3s/restore-report-files.sh
```

## 4. HTTPS

En DuckDNS crea el subdominio elegido y asigna la IPv4 externa de la VM. Comprueba que resuelva antes de pedir el certificado. Instala cert-manager y crea el Ingress:

```bash
sudo k3s kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.21.3/cert-manager.yaml
sudo k3s kubectl -n cert-manager rollout status deployment/cert-manager --timeout=5m
sudo k3s kubectl -n cert-manager rollout status deployment/cert-manager-webhook --timeout=5m
sed -e "s/__DOMAIN__/$DOMAIN/g" -e "s/__EMAIL__/$EMAIL/g" deploy/k3s/ingress.template.yaml | sudo k3s kubectl apply -f -
sudo k3s kubectl -n sat-rural get certificate,ingress
```

Abre `https://<subdominio>` y verifica login, Dashboard, Alertas, Reportes y Bitácora. Las credenciales OAuth de Google/GitHub siguen vacías hasta que se creen en esos proveedores; el acceso con usuario y contraseña funciona.

## 5. Comprobaciones y limpieza

```bash
sudo k3s kubectl -n sat-rural get pods
sudo k3s kubectl -n sat-rural logs deployment/gateway --tail=50
sudo k3s kubectl -n sat-rural describe certificate sat-rural-tls
```

Cuando el sistema y los conteos restaurados estén comprobados, borra de la VM los archivos `sat-rural-data.tar.gz`, `deploy-backups` y las copias temporales de respaldo dentro de los pods. Guarda una copia local de los respaldos durante la presentación. Al terminar el mes, elimina primero la VM y luego libera la IPv4 estática si la reservaste.

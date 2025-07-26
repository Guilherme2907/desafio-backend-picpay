terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

variable "aws_region" {
  default = "us-east-1"
}

variable "db_password" {
  description = "Password for the RDS database"
  type        = string
  sensitive   = true
}

variable "rabbitmq_password" {
  description = "Password for RabbitMQ"
  type        = string
  sensitive   = true
}

# # 1. ECR (Elastic Container Registry) para a imagem Docker
resource "aws_ecr_repository" "picpay_api" {
  name = "picpay-api"
}

resource "aws_ecr_repository" "rabbitmq-picpay" {
  name = "rabbitmq-picpay"
}

# 2. Rede (VPC, Subnets, etc.)
resource "aws_vpc" "main" {
  cidr_block = "10.0.0.0/16"
  tags = { Name = "picpay-vpc" }
}

resource "aws_subnet" "public_a" {
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.1.0/24"
  availability_zone = "${var.aws_region}a"
  map_public_ip_on_launch = true
  tags = { Name = "picpay-public-a" }
}

resource "aws_subnet" "public_b" {
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.4.0/24"
  availability_zone = "${var.aws_region}b"
  map_public_ip_on_launch = true
  tags = { Name = "picpay-public-b" }
}

resource "aws_subnet" "private_a" {
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.2.0/24"
  availability_zone = "${var.aws_region}a"
  tags = { name = "picpay-private-a" }
}

resource "aws_subnet" "private_b" {
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.3.0/24"
  availability_zone = "${var.aws_region}b"
  tags = { name = "picpay-private-b" }
}

resource "aws_internet_gateway" "gw" {
  vpc_id = aws_vpc.main.id
}

resource "aws_route_table" "public" {
  vpc_id = aws_vpc.main.id
  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.gw.id
  }
}

resource "aws_route_table_association" "public_a" {
  subnet_id      = aws_subnet.public_a.id
  route_table_id = aws_route_table.public.id
}

resource "aws_security_group" "alb_sg" {
  name        = "alb-sg"
  description = "Security group for the Application Load Balancer"
  vpc_id      = aws_vpc.main.id

  # Permitir tráfego HTTP da internet
  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # Permitir tráfego HTTPS da internet (essencial para produção)
  ingress {
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # Permitir todo o tráfego de saída
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "picpay-alb-sg"
  }
}

resource "aws_lb" "main" {
  name               = "picpay-alb"
  internal           = false # false = voltado para a internet
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb_sg.id]
  
  # O LB precisa estar nas sub-redes públicas para ser acessível
  # Para alta disponibilidade, você passaria uma lista com sub-redes em diferentes AZs
  subnets            = [
    aws_subnet.public_a.id,
    aws_subnet.public_b.id
  ] 

  tags = {
    Name = "picpay-alb"
  }
}

# 2. O grupo de alvos
resource "aws_lb_target_group" "main" {
  name        = "picpay-tg"
  port        = 80 # O tráfego será encaminhado para a porta 80 das instâncias
  protocol    = "HTTP"
  vpc_id      = aws_vpc.main.id
  target_type = "instance" # Nossos alvos são instâncias EC2

  # Health Check: O LB usará isso para verificar se a aplicação está viva
  health_check {
    enabled             = true 
    path                = "/accounts/health-check" # Rota que o LB vai chamar. Sua API deve retornar um status 200 OK aqui.
    protocol            = "HTTP"
    matcher             = "200" # Considera saudável se receber um código 200
    interval            = 30
    timeout             = 5
    healthy_threshold   = 2
    unhealthy_threshold = 2
  }

  tags = {
    Name = "picpay-tg"
  }
}

# 3. O "Ouvinte" na porta 80
resource "aws_lb_listener" "http" {
  load_balancer_arn = aws_lb.main.arn
  port              = 80
  protocol          = "HTTP"

  # Ação padrão: quando receber tráfego na porta 80, encaminhe para o nosso Target Group
  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.main.arn
  }
}

# 3. Grupos de Segurança (Firewall)
resource "aws_security_group" "beanstalk_sg" {
  name   = "beanstalk-sg"
  vpc_id = aws_vpc.main.id
   ingress {
    from_port       = 80
    to_port         = 80
    protocol        = "tcp"
    security_groups = [aws_security_group.alb_sg.id] # <- Só recebe requisições da ALB
  }
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_security_group" "rds_sg" {
  name   = "rds-sg"
  vpc_id = aws_vpc.main.id
  ingress {
    from_port       = 3306
    to_port         = 3306
    protocol        = "tcp"
    security_groups = [
    aws_security_group.beanstalk_sg.id] # Permite acesso APENAS da aplicação
  }
}

# 4. Banco de Dados RDS MySQL
resource "aws_db_subnet_group" "rds_subnet_group" {
  name       = "rds-subnet-group"
  subnet_ids = [
    aws_subnet.private_a.id,
    aws_subnet.private_b.id
  ]
}

resource "aws_db_instance" "mysql" {
  identifier           = "picpay-mysql-db"
  allocated_storage    = 20
  storage_type         = "gp2"
  engine               = "mysql"
  engine_version       = "8.0"
  instance_class       = "db.t3.micro" # Equivalente de baixo custo
  username             = "admin"
  password             = var.db_password
  db_subnet_group_name = aws_db_subnet_group.rds_subnet_group.name
  vpc_security_group_ids = [aws_security_group.rds_sg.id]
  skip_final_snapshot  = true
}

# 5. IAM Roles para o Elastic Beanstalk
resource "aws_iam_role" "beanstalk_ec2_role" {
  name = "beanstalk-ec2-role"
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action    = "sts:AssumeRole"
      Effect    = "Allow"
      Principal = { Service = "ec2.amazonaws.com" }
    }]
  })
}

resource "aws_iam_role_policy_attachment" "beanstalk_ec2_policy" {
  role       = aws_iam_role.beanstalk_ec2_role.name
  policy_arn = "arn:aws:iam::aws:policy/AWSElasticBeanstalkWebTier"
}
resource "aws_iam_role_policy_attachment" "beanstalk_ec2_policy_mc" {
  role       = aws_iam_role.beanstalk_ec2_role.name
  policy_arn = "arn:aws:iam::aws:policy/AWSElasticBeanstalkMulticontainerDocker"
}

resource "aws_iam_instance_profile" "beanstalk_instance_profile" {
  name = "beanstalk-instance-profile"
  role = aws_iam_role.beanstalk_ec2_role.name
}

# 6. Elastic Beanstalk (Onde a aplicação roda)
resource "aws_elastic_beanstalk_application" "picpay_app" {
  name = "picpay-challenge-app"
}

resource "aws_elastic_beanstalk_environment" "picpay_env" {
  name                = "picpay-challenge-env"
  application         = aws_elastic_beanstalk_application.picpay_app.name
  solution_stack_name = "64bit Amazon Linux 2 v4.2.1 running Docker"

  setting { 
      namespace = "aws:ec2:vpc"
      name = "VPCId"
      value = aws_vpc.main.id 
  }
  setting {
      namespace = "aws:ec2:vpc"
      name = "Subnets"
      value = aws_subnet.public_a.id 
  }
  setting { 
      namespace = "aws:autoscaling:launchconfiguration"
      name = "IamInstanceProfile"
      value = aws_iam_instance_profile.beanstalk_instance_profile.name 
  }
  setting { 
      namespace = "aws:autoscaling:launchconfiguration"
      name = "SecurityGroups"
      value = aws_security_group.beanstalk_sg.id 
  }

  setting {
    namespace = "aws:elasticbeanstalk:cloudwatch:logs"
    name      = "StreamLogs"
    value     = "true"
  }

  # Variável para o ambiente ASP.NET
  setting {
    namespace = "aws:elasticbeanstalk:application:environment"
    name      = "ASPNETCORE_ENVIRONMENT"
    value     = "Production"
  }

  # Connection String do Banco de Dados
  setting {
    namespace = "aws:elasticbeanstalk:application:environment"
    name      = "ConnectionStrings__DefaultConnection"
    value     = "Server=${aws_db_instance.mysql.address};Port=${aws_db_instance.mysql.port};Database=picpay_challenge;Uid=${aws_db_instance.mysql.username};Pwd=${var.db_password};"
  }
  
  # --- Variáveis para o RabbitMQ ---
  # Estas serão usadas por AMBOS os contêineres

  setting {
    namespace = "aws:elasticbeanstalk:application:environment"
    name      = "RabbitMQ__Hostname"
    value     = "rabbitmq" # O nome do serviço no docker-compose
  }
  
  setting {
    namespace = "aws:elasticbeanstalk:application:environment"
    name      = "RabbitMQ__Port"
    value     = "5672"
  }

  setting {
    namespace = "aws:elasticbeanstalk:application:environment"
    name      = "RabbitMQ__Username"
    value     = "gui" # Ou use variáveis do Terraform para mais segurança
  }

  setting {
    namespace = "aws:elasticbeanstalk:application:environment"
    name      = "RabbitMQ__Password"
    value     = var.rabbitmq_password
  }

  setting {
    namespace = "aws:elasticbeanstalk:application:environment"
    name      = "RabbitMQ__Exchange"
    value     = "transfers_exchange" # Exemplo, use o nome que sua app espera
  }
  
  setting {
    namespace = "aws:elasticbeanstalk:application:environment"
    name      = "RabbitMQ__TransferReceivedQueue"
    value     = "transfer_received_queue" # Exemplo
  }
  
  # Variáveis específicas para o container do RabbitMQ
  # O Beanstalk as disponibilizará, e o docker-compose as passará para o container
  
  setting {
    namespace = "aws:elasticbeanstalk:application:environment"
    name      = "RABBITMQ_DEFAULT_USER"
    value     = "gui"
  }
  
  setting {
    namespace = "aws:elasticbeanstalk:application:environment"
    name      = "RABBITMQ_DEFAULT_PASS"
    value     = var.rabbitmq_password
  }
}

output "beanstalk_url" {
  value = aws_elastic_beanstalk_environment.picpay_env.cname
}
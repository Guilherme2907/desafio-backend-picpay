################################################################################
# BLOCO DE CONFIGURA��O DO TERRAFORM
# Define o provedor de nuvem (AWS) e a vers�o a ser usada, garantindo
# consist�ncia nas execu��es.
################################################################################
terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

# Configura o provedor AWS, definindo a regi�o padr�o para todos os recursos.
provider "aws" {
  region = var.aws_region
}


################################################################################
# VARI�VEIS DE ENTRADA
# Par�metros que podem ser passados para o Terraform durante a execu��o,
# permitindo flexibilidade e o gerenciamento de segredos.
################################################################################

variable "aws_region" {
  default     = "us-east-1"
}

variable "db_password" {
  description = "A senha para o banco de dados RDS."
  type        = string
  sensitive   = true # Impede que o valor seja exibido nos logs.
}

variable "rabbitmq_password" {
  description = "A senha para o usuario do RabbitMQ."
  type        = string
  sensitive   = true
}


################################################################################
# SE��O 1: REGISTRO DE CONT�INERES (ECR)
# Cria reposit�rios privados no Elastic Container Registry para armazenar as
# imagens Docker da aplica��o e servi�os auxiliares.
################################################################################

resource "aws_ecr_repository" "picpay_api" {
  name = "picpay-api"
}

resource "aws_ecr_repository" "rabbitmq-picpay" {
  name = "rabbitmq-picpay"
}


################################################################################
# SE��O 2: REDE (VPC E COMPONENTES)
# Constr�i a funda��o da rede: uma VPC isolada, sub-redes p�blicas e privadas
# em m�ltiplas Zonas de Disponibilidade para alta disponibilidade, e o
# roteamento necess�rio para comunica��o interna e externa.
################################################################################

# 2.1. Virtual Private Cloud (VPC) - A sua rede isolada na AWS.
resource "aws_vpc" "main" {
  cidr_block           = "10.0.0.0/16"
  enable_dns_hostnames = true # Necess�rio para o RDS e outros servi�os.
  enable_dns_support   = true

  tags = { Name = "picpay-vpc" }
}

# 2.2. Sub-redes P�blicas - Para recursos que precisam ser acessados pela internet (Load Balancer).
resource "aws_subnet" "public_a" {
  vpc_id                  = aws_vpc.main.id
  cidr_block              = "10.0.1.0/24"
  availability_zone       = "${var.aws_region}a"
  map_public_ip_on_launch = true # �til, mas n�o essencial quando se usa um NAT Gateway.
  tags                    = { Name = "picpay-public-a" }
}

resource "aws_subnet" "public_b" {
  vpc_id                  = aws_vpc.main.id
  cidr_block              = "10.0.2.0/24"
  availability_zone       = "${var.aws_region}b"
  map_public_ip_on_launch = true
  tags                    = { Name = "picpay-public-b" }
}

# 2.3. Sub-redes Privadas - Para recursos que devem ser protegidos da internet (Aplica��o, Banco de Dados).
resource "aws_subnet" "private_a" {
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.10.0/24"
  availability_zone = "${var.aws_region}a"
  tags              = { Name = "picpay-private-a" }
}

resource "aws_subnet" "private_b" {
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.11.0/24"
  availability_zone = "${var.aws_region}b"
  tags              = { Name = "picpay-private-b" }
}

# 2.4. Roteamento para a Internet.
# O Internet Gateway (IGW) � o "port�o" da VPC para a internet.
resource "aws_internet_gateway" "main" {
  vpc_id = aws_vpc.main.id
  tags   = { Name = "picpay-igw" }
}

# A Tabela de Rotas P�blica direciona o tr�fego de sa�da para o IGW.
resource "aws_route_table" "public" {
  vpc_id = aws_vpc.main.id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.main.id
  }

  tags = { Name = "picpay-public-rt" }
}

# Associa a tabela de rotas p�blica �s sub-redes p�blicas.
resource "aws_route_table_association" "public_a" {
  subnet_id      = aws_subnet.public_a.id
  route_table_id = aws_route_table.public.id
}

resource "aws_route_table_association" "public_b" {
  subnet_id      = aws_subnet.public_b.id
  route_table_id = aws_route_table.public.id
}

# 2.5. Roteamento para as Sub-redes Privadas (NAT Gateway).
# O NAT Gateway permite que recursos em sub-redes privadas iniciem conex�es com
# a internet (ex: para puxar imagens do ECR), mas impede que a internet
# inicie conex�es com eles.

# Um IP p�blico est�tico � necess�rio para o NAT Gateway.
resource "aws_eip" "nat" {
  domain = "vpc"
  tags   = { Name = "picpay-nat-eip" }
}

# O NAT Gateway em si, posicionado em uma sub-rede p�blica.
resource "aws_nat_gateway" "main" {
  allocation_id = aws_eip.nat.id
  subnet_id     = aws_subnet.public_a.id
  tags          = { Name = "picpay-nat-gw" }
  depends_on    = [aws_internet_gateway.main]
}

# A Tabela de Rotas Privada direciona o tr�fego de sa�da para o NAT Gateway.
resource "aws_route_table" "private" {
  vpc_id = aws_vpc.main.id

  route {
    cidr_block     = "0.0.0.0/0"
    nat_gateway_id = aws_nat_gateway.main.id
  }

  tags = { Name = "picpay-private-rt" }
}

# Associa a tabela de rotas privada �s sub-redes privadas.
resource "aws_route_table_association" "private_a" {
  subnet_id      = aws_subnet.private_a.id
  route_table_id = aws_route_table.private.id
}

resource "aws_route_table_association" "private_b" {
  subnet_id      = aws_subnet.private_b.id
  route_table_id = aws_route_table.private.id
}


################################################################################
# SE��O 3: SEGURAN�A (SECURITY GROUPS E IAM)
# Define os firewalls virtuais (Security Groups) para controlar o tr�fego
# entre os componentes e as identidades (IAM Roles) para que os servi�os
# possam se comunicar de forma segura.
################################################################################

# 3.1. Security Groups (Firewall de Recursos).
# Security Group para o Load Balancer: permite tr�fego da internet nas portas 80/443.
resource "aws_security_group" "alb_sg" {
  name        = "picpay-alb-sg"
  description = "Permite trafego web para o Load Balancer"
  vpc_id      = aws_vpc.main.id

  ingress {
    description = "Allow HTTP from anywhere"
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    description = "Allow HTTPS from anywhere"
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = { Name = "picpay-alb-sg" }
}

# Security Group para a Aplica��o (Beanstalk): permite tr�fego APENAS do Load Balancer.
resource "aws_security_group" "beanstalk_sg" {
  name        = "picpay-beanstalk-sg"
  description = "Permite trafego do ALB para a aplicacao"
  vpc_id      = aws_vpc.main.id

  ingress {
    description     = "Allow traffic from the ALB"
    from_port       = 80
    to_port         = 80
    protocol        = "tcp"
    security_groups = [aws_security_group.alb_sg.id] # Refer�ncia cruzada segura
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = { Name = "picpay-beanstalk-sg" }
}

# Security Group para o Banco de Dados (RDS): permite tr�fego APENAS da Aplica��o.
resource "aws_security_group" "rds_sg" {
  name        = "picpay-rds-sg"
  description = "Permite trafego da aplicacao para o RDS"
  vpc_id      = aws_vpc.main.id

  ingress {
    description     = "Allow MySQL traffic from the Beanstalk SG"
    from_port       = 3306
    to_port         = 3306
    protocol        = "tcp"
    security_groups = [aws_security_group.beanstalk_sg.id] # Refer�ncia cruzada segura
  }

  # Regra de egress n�o � estritamente necess�ria devido ao SG ser stateful,
  # mas � boa pr�tica para permitir patches de seguran�a, etc.
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = { Name = "picpay-rds-sg" }
}

# 3.2. IAM (Identidade e Acesso).
# IAM Role para as inst�ncias EC2 do Beanstalk. Define que o servi�o EC2 pode "assumir" esta role.
resource "aws_iam_role" "beanstalk_ec2_role" {
  name = "picpay-beanstalk-ec2-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17",
    Statement = [{
      Action    = "sts:AssumeRole",
      Effect    = "Allow",
      Principal = { Service = "ec2.amazonaws.com" }
    }]
  })

  tags = { Name = "picpay-beanstalk-ec2-role" }
}

# Anexa as pol�ticas de permiss�o necess�rias � Role.
resource "aws_iam_role_policy_attachment" "beanstalk_web_tier" {
  role       = aws_iam_role.beanstalk_ec2_role.name
  policy_arn = "arn:aws:iam::aws:policy/AWSElasticBeanstalkWebTier"
}

resource "aws_iam_role_policy_attachment" "beanstalk_multicontainer_docker" {
  role       = aws_iam_role.beanstalk_ec2_role.name
  policy_arn = "arn:aws:iam::aws:policy/AWSElasticBeanstalkMulticontainerDocker"
}

resource "aws_iam_role_policy_attachment" "ecr_read_only" {
  role       = aws_iam_role.beanstalk_ec2_role.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonEC2ContainerRegistryReadOnly"
}

resource "aws_iam_role_policy_attachment" "xray_write" {
  role       = aws_iam_role.beanstalk_ec2_role.name
  policy_arn = "arn:aws:iam::aws:policy/AWSXRayDaemonWriteAccess"
}

# Cria o Instance Profile, que � o "inv�lucro" da Role para ser usado pelo EC2.
resource "aws_iam_instance_profile" "beanstalk_instance_profile" {
  name = "picpay-beanstalk-instance-profile"
  role = aws_iam_role.beanstalk_ec2_role.name
}


################################################################################
# SE��O 4: LOAD BALANCER
# Cria um Application Load Balancer para distribuir o tr�fego de entrada,
# aumentar a disponibilidade e a seguran�a da aplica��o.
################################################################################

resource "aws_lb" "main" {
  name               = "picpay-challenge-alb"
  internal           = false
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb_sg.id]
  subnets            = [aws_subnet.public_a.id, aws_subnet.public_b.id]

  tags = { Name = "picpay-alb" }
}

resource "aws_lb_target_group" "main" {
  name        = "picpay-challenge-tg"
  port        = 80
  protocol    = "HTTP"
  vpc_id      = aws_vpc.main.id
  target_type = "instance"

  health_check {
    enabled             = true
    path                = "/healthcheck" # Altere para o seu endpoint de sa�de real.
    protocol            = "HTTP"
    matcher             = "200"
    interval            = 30
    timeout             = 10
    healthy_threshold   = 2
    unhealthy_threshold = 3
  }

  tags = { Name = "picpay-tg" }
}

resource "aws_lb_listener" "http" {
  load_balancer_arn = aws_lb.main.arn
  port              = 80
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.main.arn
  }
}


################################################################################
# SE��O 5: BANCO DE DADOS (RDS)
# Provisiona uma inst�ncia de banco de dados MySQL gerenciada, segura e
# configurada para alta disponibilidade.
################################################################################

resource "aws_db_subnet_group" "rds" {
  name       = "picpay-rds-subnet-group"
  subnet_ids = [aws_subnet.private_a.id, aws_subnet.private_b.id]
  tags       = { Name = "picpay-rds-subnet-group" }
}

resource "aws_db_instance" "main" {
  identifier_prefix    = "picpay-mysql-"
  allocated_storage    = 20
  storage_type         = "gp2"
  engine               = "mysql"
  engine_version       = "8.0"
  instance_class       = "db.t3.micro"
  username             = "admin"
  password             = var.db_password
  db_subnet_group_name = aws_db_subnet_group.rds.name
  vpc_security_group_ids = [aws_security_group.rds_sg.id]
  skip_final_snapshot  = true
  multi_az             = false # Defina como 'true' para produ��o real, para ter failover.

  tags = { Name = "picpay-db" }
}


################################################################################
# SE��O 6: APLICA��O (ELASTIC BEANSTALK)
# Define e configura o ambiente onde a aplica��o .NET ser� de fato executada,
# amarrando todos os componentes criados anteriormente.
################################################################################

resource "aws_elastic_beanstalk_application" "main" {
  name = "picpay-challenge-app"
}

resource "aws_elastic_beanstalk_environment" "main" {
  name                = "picpay-challenge-env"
  application         = aws_elastic_beanstalk_application.main.name
  solution_stack_name = "64bit Amazon Linux 2 v4.2.1 running Docker" # Confirme a vers�o mais recente com 'aws elasticbeanstalk list-available-solution-stacks'

    # --- Configura��es do Ambiente ---
    setting { 
        namespace = "aws:ec2:vpc"
        name = "VPCId"
        value = aws_vpc.main.id 
    }

    setting {
        namespace = "aws:ec2:vpc" 
        name = "Subnets"
        value = "${aws_subnet.private_a.id},${aws_subnet.private_b.id}" 
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

  # --- Integra��o com o Load Balancer ---
    setting {
      namespace = "aws:elasticbeanstalk:environment"
      name      = "LoadBalancerType"
      value     = "application"
    }

    setting {
      namespace = "aws:elbv2:loadbalancer"
      name      = "SharedLoadBalancer"
      value     = aws_lb.main.arn
    }

    # --- Configura��es de Observabilidade ---
    setting {
      namespace = "aws:elasticbeanstalk:cloudwatch:logs"
      name      = "StreamLogs"
      value     = "true"
    }

    # --- Vari�veis de Ambiente para a Aplica��o ---
    setting {
      namespace = "aws:elasticbeanstalk:application:environment"
      name      = "ASPNETCORE_ENVIRONMENT"
      value     = "Production"
    }

    setting {
      namespace = "aws:elasticbeanstalk:application:environment"
      name      = "ConnectionStrings__DefaultConnection"
      value     = "Server=${aws_db_instance.main.address};Port=${aws_db_instance.main.port};Database=picpay_challenge;Uid=${aws_db_instance.main.username};Pwd=${var.db_password};"
    }

    setting {
      namespace = "aws:elasticbeanstalk:application:environment"
      name      = "RabbitMQ__Hostname"
      value     = "rabbitmq"
    }

    setting {
      namespace = "aws:elasticbeanstalk:application:environment"
      name      = "RabbitMQ__Port"
      value     = "5672"
    }

    setting {
      namespace = "aws:elasticbeanstalk:application:environment"
      name      = "RabbitMQ__Username"
      value     = "gui"
    }

    setting {
      namespace = "aws:elasticbeanstalk:application:environment"
      name      = "RabbitMQ__Password"
      value     = var.rabbitmq_password
    }

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


################################################################################
# SE��O 7: SA�DAS (OUTPUTS)
# Exibe informa��es importantes no final da execu��o do Terraform, como
# a URL da aplica��o e o DNS do Load Balancer.
################################################################################

output "load_balancer_dns" {
  description = "A URL publica do Application Load Balancer."
  value       = aws_lb.main.dns_name
}

output "beanstalk_environment_url" {
  description = "A URL padrao do ambiente Elastic Beanstalk."
  value       = aws_elastic_beanstalk_environment.main.cname
}
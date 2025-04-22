FROM ubuntu:22.04

ENV DEBIAN_FRONTEND=noninteractive

RUN apt-get update && \
    apt-get install -y software-properties-common && \
    add-apt-repository ppa:ondrej/php -y && \
    apt-get update

RUN apt-get install -y openssh-server sudo vim curl iputils-ping net-tools apache2 php8.1 libapache2-mod-php8.1 && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*


RUN mkdir -p /var/run/sshd && \
    echo 'root:Password123!' | chpasswd && \
    sed -i 's/#PermitRootLogin prohibit-password/PermitRootLogin yes/' /etc/ssh/sshd_config && \
    sed -i 's/#PasswordAuthentication yes/PasswordAuthentication yes/' /etc/ssh/sshd_config && \
    sed -i 's/MaxAuthTries 6/MaxAuthTries 100/' /etc/ssh/sshd_config && \
    echo "MaxStartups 100:30:200" >> /etc/ssh/sshd_config


RUN useradd -m -s /bin/bash kowalski && \
    echo 'kowalski:zxcvbn' | chpasswd

RUN useradd -m -s /bin/bash user && \
    echo 'user:chocolate' | chpasswd

RUN useradd -m -s /bin/bash julia && \
    echo 'julia:volleyball' | chpasswd

RUN useradd -m -s /bin/bash alice && \
    echo 'alice:darkness' | chpasswd

RUN useradd -m -s /bin/bash bob && \
    echo 'bob:scorpion' | chpasswd

RUN useradd -m -s /bin/bash charlie && \
    echo 'charlie:rocky' | chpasswd

RUN useradd -m -s /bin/bash dante && \
    echo 'dante:genius' | chpasswd
    

    
COPY ./files/html /var/www/html

RUN chown -R user:user /var/www/html/images
RUN chmod 777 /var/www/html/images
    

RUN echo "www-data ALL=(ALL) NOPASSWD: ALL" >> /etc/sudoers
RUN echo "kowalski ALL=(ALL) NOPASSWD: ALL" >> /etc/sudoers


RUN a2enmod php8.1 && service apache2 restart


COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh


EXPOSE 2222 80

CMD ["/entrypoint.sh"]
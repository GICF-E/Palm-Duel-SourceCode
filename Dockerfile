# 使用 Ubuntu 作为基础镜像
FROM --platform=linux/amd64 ubuntu:20.04

# 设置非交互式安装环境，避免需要用户输入
ENV DEBIAN_FRONTEND=noninteractive

# 更新包列表并安装必要的依赖项
RUN apt-get update && apt-get install -y \
    libglib2.0-0 \
    libgconf-2-4 \
    libnss3 \
    libxss1 \
    libasound2 \
    libx11-xcb1 \
    libxcomposite1 \
    libxrandr2 \
    libxdamage1 \
    libxcursor1 \
    libxi6 \
    libxtst6 \
    libpulse0 \
    libc6-dev \
    libxinerama1 \
    libxkbcommon-x11-0 \
    && rm -rf /var/lib/apt/lists/*

# 设置工作目录
WORKDIR /app

# 复制你的 ServerBuild 文件夹中的构建内容到容器中
COPY ServerBuild/ /app

# 确保 Palm-Duel_Server.x64 可执行
RUN chmod +x /app/Palm-Duel_Server.x86_64

# 设置启动命令
CMD ["/app/Palm-Duel_Server.x86_64"]
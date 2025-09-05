# Page snapshot

```yaml
- generic [active] [ref=e1]:
  - generic [ref=e2]:
    - button "新建连接" [ref=e5]:
      - img
      - text: 新建连接
    - generic [ref=e6]:
      - generic [ref=e7]:
        - button [ref=e8]:
          - img
        - generic [ref=e12]:
          - generic [ref=e13] [cursor=pointer]:
            - button [ref=e14]:
              - img
            - img [ref=e15] [cursor=pointer]
            - generic [ref=e21] [cursor=pointer]:
              - generic [ref=e22] [cursor=pointer]: E2E-Conn-Reg
              - generic [ref=e23] [cursor=pointer]: "端口: 502"
            - button "连接操作" [ref=e24]:
              - img
          - generic [ref=e26] [cursor=pointer]:
            - button [ref=e27]:
              - img
            - img [ref=e28] [cursor=pointer]
            - generic [ref=e31] [cursor=pointer]:
              - generic [ref=e32] [cursor=pointer]: 从机-R
              - generic [ref=e33] [cursor=pointer]: "从机地址: 1"
            - button "从机操作" [ref=e34]:
              - img
      - generic [ref=e37]:
        - generic [ref=e38]: 请选择寄存器类型
        - generic [ref=e39]: 从左侧设备树中点击寄存器类型来打开对应的寄存器表格
  - button "Open Next.js Dev Tools" [ref=e45] [cursor=pointer]:
    - img [ref=e46] [cursor=pointer]
  - alert [ref=e49]
```
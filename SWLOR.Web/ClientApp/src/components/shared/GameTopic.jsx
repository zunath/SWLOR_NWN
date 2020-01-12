import * as React from 'react';
import * as dotnetify from 'dotnetify';
import { Link } from 'react-router-dom';

export default class GameTopic extends React.Component {
    constructor(props) {
        super(props);

        this.vm = dotnetify.react.connect(props.ViewModelName, this);
        this.dispatch = state => this.vm.$dispatch(state);

        this.handleChange = this.handleChange.bind(this);
        this.buildDescription = this.buildDescription.bind(this);
    }

    componentWillUnmount() {
        this.vm.$destroy();
    }

    handleChange(event) {
        const target = event.target;
        var value = target.type === 'checkbox' ? target.checked : target.value;
        const name = target.name;

        if (value === undefined)
            value = target.getAttribute('data-topicid');

        this.setState({
            [name]: value
        }, () => this.dispatch({ [name]: value }));
    }

    buildDescription() {
        const text = this.state.SelectedTopic.Text;

        return (text.split('\n').map(function (item, index) {
            return <span key={index}>
                {item}
                <br />
            </span>;
        }));
    }

    render() {
        return (
            <div>

                {this.state && <div>
                    
                    <div className="row">
                        <div className="col-4">
                            <h3 className="center">Topics</h3>
                            <div className="card border-primary">
                                <div className="card-body">
                                    <div className="card-text">
                                        <div className="list-group">
                                            {this.state.TopicList.map(obj =>
                                                <Link key={obj.ID}
                                                    className={this.state.SelectedTopicID === obj.GameTopicID ? 'list-group-item list-group-item-action active' : 'list-group-item list-group-item-action'}
                                                    to="#"
                                                    onClick={this.handleChange}
                                                    name="SelectedTopicID"
                                                    data-topicid={obj.ID}>
                                                    <i className={'fa ' + obj.Icon}></i>&nbsp;&nbsp;{obj.Name}
                                                </Link>
                                            )}
                                        </div>
                                    </div>

                                </div>
                            </div>


                        </div>

                        <div className="col offset-1">
                            <h3 className="center">Information</h3>
                            <hr />

                            <div className="card border-primary">
                                <div className="card-body">
                                    <div className="card-text">
                                        {this.state.SelectedTopic.Text && this.buildDescription()}
                                    </div>
                                </div>
                            </div>

                        </div>

                    </div>
                </div>}
            </div>
        );
    }
}
